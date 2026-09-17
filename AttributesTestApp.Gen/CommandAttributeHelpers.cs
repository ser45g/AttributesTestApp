namespace AttributesTestApp.Gen
{
    public static class CommandAttributeHelpers
    {
        public const string AttributeCode = """
        namespace AttributesTestApp.Gen.Attributes
        {
            [AttributeUsage(AttributeTargets.Class)]
            public class CommandAttribute : Attribute
            {
               public string Name { get; set; }
               public string? Description { get; set; }
            }
        }
        """;
        public const string AttributeShortName = "Command";
        public const string AttributeFullName = AttributeShortName + "Attribute";
        public const string AttributeMetadataName = $"AttributesTestApp.Gen.Attributes.{AttributeFullName}";
    }
}
