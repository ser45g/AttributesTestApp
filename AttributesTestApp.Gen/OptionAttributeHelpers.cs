namespace AttributesTestApp.Gen
{
    public static class OptionAttributeHelpers
    {
        public const string AttributeCode = """
        namespace AttributesTestApp.Gen.Attributes
        {
            [AttributeUsage(AttributeTargets.Property)]
            public class OptionAttribute : Attribute
            {
                public string ShortName { get; set; }
                public string LongName { get; set; }
                public bool IsRequired { get; set; }
                public string? Description { get; set; }
            }
        }
        """;
        public const string AttributeShortName = "Option";
        public const string AttributeFullName = AttributeShortName + "Attribute";
        public const string AttributeMetadataName = "AttributesTestApp.Gen.Attributes." + AttributeFullName;
    }
}
