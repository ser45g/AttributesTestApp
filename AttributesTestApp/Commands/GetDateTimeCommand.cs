using AttributesTestApp.Gen.Attributes;
using AttributesTestApp.Services;

namespace AttributesTestApp.Commands
{
    [Command(Name = "datetime", Description = "Get the current date and time")]
    public class GetDateTimeCommand(IDateTimeProvider dateTimeProvider): ICommand
    {
       [Option(ShortName = "u", LongName = "utc", Description = "Get the current date and time in the utc format")]
        public bool IsUtc { get; set; }

        public async ValueTask Execute(CancellationToken cancellationToken)
        {
            var dateTime = await dateTimeProvider.DateTimeUtc;

            var formattedDateTimeString = IsUtc ? dateTime.ToString("U") : dateTime.ToLocalTime().ToString("G");

            Console.WriteLine($"Current date and time is: {formattedDateTimeString}");
        }
    }
}
