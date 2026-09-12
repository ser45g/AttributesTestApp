
using AttributesTestApp.Attributes;

namespace AttributesTestApp.Commands
{
    [Command("long")]
    public class LongCommand : ICommand
    {
        public async ValueTask Execute(CancellationToken cancellationToken)
        {
            await Task.Delay(5500, cancellationToken);

            Console.WriteLine("A really long task");
        }
    }
}
