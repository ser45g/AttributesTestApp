using AttributesTestApp.Attributes;
using System.Reflection;
using System.Xml.Linq;

namespace AttributesTestApp
{
    public class CommandEngine
    {
        private readonly Dictionary<string, Type> _commands;

        private CommandEngine(Dictionary<string, Type> commands)
        {
           _commands = commands;
        }

        public static CommandEngine Create()
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

            return new CommandEngine(commands);
        }

        public void Run(string[] args)
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

            try
            {
                var command = Activator.CreateInstance(commandType);

                ParseArguments(command, commandArgs);

                var executeMethod = commandType.GetMethod("Execute");

                executeMethod?.Invoke(command, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void ParseArguments(object command, string[] args)
        {
            var properties = command.GetType().GetProperties();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                // Check if this is an option (starts with - or --)
                if (!arg.StartsWith("-")) continue;

                // Find the matching property
                foreach (var prop in properties)
                {
                    var attr = prop.GetCustomAttribute<OptionAttribute>();

                    if (attr == null) continue;

                    // Check if this option matches the current argument
                    var shortMatch = $"-{attr.ShortName}" == arg || $"--{attr.ShortName}" == arg;
                    var longMatch = $"--{attr.LongName}" == arg || $"-{attr.LongName}" == arg;

                    if (shortMatch || longMatch)
                    {
                        // Get the value (next argument)
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            var value = args[i + 1];
                            prop.SetValue(command, Convert.ChangeType(value, prop.PropertyType));
                            i++; // Skip the value in the next iteration
                        }
                        else if (prop.PropertyType == typeof(bool))
                        {
                            // Boolean flags don't need a value
                            prop.SetValue(command, true);
                        }
                        break;
                    }
                }
            }

            // Validate required options
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
