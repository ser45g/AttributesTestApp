namespace AttributesTestApp.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OptionAttribute : Attribute
    {
        public string ShortName { get; init; }
        public string LongName { get; init; }
        public string? Description { get; init; }
        public bool IsRequired { get; init; }

        public OptionAttribute(string shortName, string longName, bool isRequired = true, string? description = null)
        {
            ShortName = shortName;
            LongName = longName;
            Description = description;
            IsRequired = isRequired;
        }
    }
}
