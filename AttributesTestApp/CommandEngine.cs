using AttributesTestApp.Commands;
using AttributesTestApp.Gen;
using AttributesTestApp.Gen.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AttributesTestApp
{
    public class CommandEngine
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CommandEngine(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public static CommandEngine Create(IServiceScopeFactory serviceScopeFactory)
        {

            return new CommandEngine( serviceScopeFactory);
        }

        public async Task Run(string[] args)
        {
            if (args.Length == 0 || (args[0] == "-h" || args[0]== "--help"))
            {
                ShowHelp();
                return;
            }

            var commandName = args[0].ToLower();

            var commandArgs = args.Skip(1).ToArray();

            if (!CommandDescriptorsStorage.Commands.TryGetValue(commandName, out var commandDescriptor))
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
            };

            Console.CancelKeyPress += cancelEventHandler;

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var command = (ICommand)ActivatorUtilities.CreateInstance(scope.ServiceProvider, commandDescriptor.Type);

                CommandArgumentBinder.Bind(command, commandDescriptor, commandArgs);

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

        private void ShowHelp()
        {
            Console.WriteLine("Available commands:");

            foreach (var (_, commandDescriptor) in CommandDescriptorsStorage.Commands)
            {
                PrintCommand(commandDescriptor);
            }
        }

        private void PrintOption(OptionDescriptor optionDescriptor)
        {
            if (optionDescriptor != null)
            {
                Console.WriteLine((string.IsNullOrWhiteSpace(optionDescriptor.Description) ? 
                        $"    -{optionDescriptor.ShortName}|--{optionDescriptor.LongName}  {optionDescriptor.Description}":
                        $"    -{optionDescriptor.ShortName}|--{optionDescriptor.LongName}")
                    + (optionDescriptor.IsRequired ? " [REQUIRED]" : ""));
            }
        }

        private void PrintCommand(CommandDescriptor commandDescriptor)
        {
            Console.WriteLine(string.IsNullOrWhiteSpace(commandDescriptor.Description) ?
                    $"  {commandDescriptor.Name}" :
                    $"  {commandDescriptor.Name} - {commandDescriptor.Description}");

            foreach (var option in commandDescriptor.Options)
            {
                PrintOption(option);
            }
            Console.WriteLine();
        }
    }
}
