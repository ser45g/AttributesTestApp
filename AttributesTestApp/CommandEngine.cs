using AttributesTestApp.Attributes;
using AttributesTestApp.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AttributesTestApp
{
    public class CommandEngine
    {
        private readonly Dictionary<string, Type> _commands;
        private readonly IServiceScopeFactory _scopeFactory;

        public CommandEngine(Dictionary<string, Type> commands, IServiceScopeFactory scopeFactory)
        {
            _commands = commands;
            _scopeFactory = scopeFactory;
        }

        public static CommandEngine Create(IServiceScopeFactory serviceScopeFactory)
        {
            Dictionary<string, Type> commands = new();

            var commandTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<CommandAttribute>() != null);

            foreach (var type in commandTypes)
            {
                var attr = type.GetCustomAttribute<CommandAttribute>();

                if(attr != null)
                    commands[attr.Name.ToLower()] = type;
            }

            return new CommandEngine(commands, serviceScopeFactory);
        }

        public async Task Run(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            var commandName = args[0].ToLower();

            var commandArgs = args.Skip(1).ToArray();

            if (!_commands.TryGetValue(commandName, out var commandType))
            {
                Console.WriteLine($"Unknown command: {commandName}");
                ShowHelp();
                return;
            }


            using CancellationTokenSource tokenSource = new();

            ConsoleCancelEventHandler cancelEventHandler = (object? sender, ConsoleCancelEventArgs e) =>
            {
                e.Cancel = true;
                tokenSource.Cancel();
                Console.WriteLine("Execution was stopped");
            };

            Console.CancelKeyPress += cancelEventHandler;

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var command = (ICommand)ActivatorUtilities.CreateInstance(scope.ServiceProvider, commandType);

                ParseArguments(command, commandArgs); //would be good if it's executed before creating scopes and all that

                await command.Execute(tokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("The operation was cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                Console.CancelKeyPress -= cancelEventHandler;
            }
        }

        private void ParseArguments(ICommand command, string[] args)
        {
            var properties = command.GetType().GetProperties();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (!arg.StartsWith("-")) continue;

                foreach (var prop in properties)
                {
                    var attr = prop.GetCustomAttribute<OptionAttribute>();

                    if (attr == null) continue;

                    var shortMatch = $"-{attr.ShortName}" == arg || $"--{attr.ShortName}" == arg;
                    var longMatch = $"--{attr.LongName}" == arg || $"-{attr.LongName}" == arg;

                    if (shortMatch || longMatch)
                    {
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            var value = args[i + 1];
                            prop.SetValue(command, Convert.ChangeType(value, prop.PropertyType));
                        }
                        else if (prop.PropertyType == typeof(bool))
                        {
                            prop.SetValue(command, true);
                        }
                        break;
                    }
                }
            }

            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<OptionAttribute>();
                if (attr?.IsRequired == true && prop.GetValue(command) == null)
                {
                    throw new Exception($"Option -{attr.ShortName}|--{attr.LongName} is required");
                }
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine("Available commands:");

            foreach (var (name, type) in _commands)
            {
                var attr = type.GetCustomAttribute<CommandAttribute>();

                PrintCommand(type, name, attr);
            }
        }

        private void PrintOption(OptionAttribute? optAttr)
        {
            if (optAttr != null)
            {
                Console.WriteLine(
                    (string.IsNullOrWhiteSpace(optAttr.Description) ? 
                        $"    -{optAttr.ShortName}|--{optAttr.LongName}  {optAttr.Description}":
                        $"    -{optAttr.ShortName}|--{optAttr.LongName}")
                    + (optAttr.IsRequired ? " [REQUIRED]" : ""));
            }
        }

        private void PrintCommand(Type type, string name, CommandAttribute? attr)
        {
            if (attr == null)
                return;

            Console.WriteLine(string.IsNullOrWhiteSpace(attr.Description) ?
                    $"  {name}" :
                    $"  {name} - {attr.Description}");

            var properties = type.GetProperties();

            foreach (var prop in properties)
            {
                var optAttr = prop.GetCustomAttribute<OptionAttribute>();

                PrintOption(optAttr);
            }
            Console.WriteLine();
        }
    }
}
