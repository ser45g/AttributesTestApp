namespace AttributesTestApp.Services
{
    public interface IDateTimeProvider
    {
        Task<DateTime> DateTimeUtc { get; }
    }
}
