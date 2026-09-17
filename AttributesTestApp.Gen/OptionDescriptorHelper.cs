
namespace AttributesTestApp.Gen
{
    public class OptionDescriptorHelper
    {
        public const string Code = """

        namespace AttributesTestApp.Gen
        {
            public class OptionDescriptor
            {
                public string ShortName { get; set; }
                public string LongName { get; set; }
                public string? Description { get; set; }
                public bool IsRequired { get; set; }
                public Type PropertyType { get; set; }
                public string PropertyName {get; set; }
            }
        }
        """;
    }
}
