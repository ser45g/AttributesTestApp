
namespace AttributesTestApp.Gen
{
    public class CommandDescriptorHelper 
    {
        public const string Code = """
        
        namespace AttributesTestApp.Gen
        {
            public class CommandDescriptor 
            {
                public string Name { get; set; }
        
                public Type Type { get; set; }
        
                public string? Description { get; set; } = null;
        
                public ICollection<OptionDescriptor> Options { get; set; } = [];
            }
        }
        
        """;
    }
}
