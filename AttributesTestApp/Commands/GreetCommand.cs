using AttributesTestApp.Gen.Attributes;

namespace AttributesTestApp.Commands
{
    [Command(Name = "greet", Description = "Greets a user by name")]
    public class GreetCommand: ICommand
    {
        [Option(ShortName = "n", LongName = "name", Description = "Your name", IsRequired = true)]
        public string Name { get; set; }

        [Option(ShortName = "c", LongName = "count", Description = "How many times to greet")]
        public int Count { get; set; } = 1;

        [Option(ShortName = "u",LongName = "uppercase", Description = "Greet in UPPERCASE")]
        public bool Uppercase { get; set; }

        public ValueTask Execute(CancellationToken cancellationToken)
        {
            var greeting = $"Hello, {Name}!";
            if (Uppercase) greeting = greeting.ToUpper();

            for (int i = 0; i < Count; i++)
            {
                Console.WriteLine(greeting);
            }

            return ValueTask.CompletedTask;
        }
    }
}
