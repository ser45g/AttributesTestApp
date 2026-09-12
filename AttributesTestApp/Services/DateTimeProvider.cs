namespace AttributesTestApp.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public Task<DateTime> DateTimeUtc => Task.Run(()=>DateTime.UtcNow);

    }
}
