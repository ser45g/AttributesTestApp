namespace AttributesTestApp.Attributes
{
    // CommandAttribute.cs
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; init; }
        public string? Description { get; init; }

        public CommandAttribute(string name, string? description = null)
        {
            Name = name;
            Description = description;
        }
    }
}
