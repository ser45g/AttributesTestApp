namespace AttributesTestApp.Commands
{
    public interface ICommand
    {
        ValueTask Execute(CancellationToken cancellationToken);
    }
}
