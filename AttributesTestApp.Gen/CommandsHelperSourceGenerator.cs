using AttributesTestApp.Gen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

[Generator]
public sealed class CommandsHelperSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource("CommandAttribute.g.cs",
                SourceText.From(CommandAttributeHelpers.AttributeCode, Encoding.UTF8)));

        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource("OptionAttribute.g.cs",
                SourceText.From(OptionAttributeHelpers.AttributeCode, Encoding.UTF8)));

        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource("CommandDescriptor.g.cs",
                SourceText.From(CommandDescriptorHelper.Code, Encoding.UTF8)));

        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource("OptionDescriptor.g.cs",
                SourceText.From(OptionDescriptorHelper.Code, Encoding.UTF8)));

        var compilationAndClasses = context.CompilationProvider
            .Combine(context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cds
                    && cds.AttributeLists.Count > 0
                    && !ContainsErrors(cds),
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
                .Collect());

        context.RegisterSourceOutput(compilationAndClasses, static (spc, pair) =>
        {
            var (compilation, classes) = pair;

            var commandAttrSymbol = compilation.GetTypeByMetadataName(
                CommandAttributeHelpers.AttributeMetadataName);

            if (commandAttrSymbol is null)
                return;

            var commandInfos = CollectCommands(compilation, classes, commandAttrSymbol);

            EmitDescriptors(spc, commandInfos);
            EmitBinder(spc, commandInfos);
        });
    }

    // ------------------------------------------------------------------
    //  Collection
    // ------------------------------------------------------------------

    private sealed class OptionInfo
    {
        public OptionInfo(
            string longName,
            string? shortName,
            string propertyName,
            bool isRequired,
            ITypeSymbol propertyType)
        {
            LongName = longName;
            ShortName = shortName;
            PropertyName = propertyName;
            IsRequired = isRequired;
            PropertyType = propertyType;
        }

        public string LongName { get; }
        public string? ShortName { get; }
        public string PropertyName { get; }
        public bool IsRequired { get; }
        public ITypeSymbol PropertyType { get; }
    }

    private sealed class CommandInfo
    {
        public CommandInfo(
            string name,
            string description,
            string typeRef,
            ImmutableArray<OptionInfo> options)
        {
            Name = name;
            Description = description;
            TypeRef = typeRef;
            Options = options;
        }

        public string Name { get; }
        public string Description { get; }
        public string TypeRef { get; }
        public ImmutableArray<OptionInfo> Options { get; }
    }

    private static List<CommandInfo> CollectCommands(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classes,
        INamedTypeSymbol commandAttrSymbol)
    {
        var result = new List<CommandInfo>();

        foreach (var classDecl in classes)
        {
            var model = compilation.GetSemanticModel(classDecl.SyntaxTree);

            if (model.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol type)
                continue;

            var attr = type.GetAttributes().FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, commandAttrSymbol));

            if (attr is null)
                continue;

            string? commandName = null;
            string? description = null;

            foreach (var arg in attr.NamedArguments)
            {
                if (arg.Key == "Name")
                    commandName = arg.Value.Value as string;
                if (arg.Key == "Description")
                    description = arg.Value.Value as string;
            }

            var options = ImmutableArray.CreateBuilder<OptionInfo>();

            foreach (var member in type.GetMembers())
            {
                if (member is not IPropertySymbol prop) continue;
                if (prop.DeclaredAccessibility != Accessibility.Public) continue;
                if (prop.SetMethod is null) continue;

                var optAttr = prop.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString() == OptionAttributeHelpers.AttributeMetadataName);

                if (optAttr is null) continue;

                string longName = optAttr.NamedArguments
                    .FirstOrDefault(x => x.Key == "LongName").Value.Value as string
                    ?? $"--{prop.Name.ToLowerInvariant()}";

                string? shortName = optAttr.NamedArguments
                    .FirstOrDefault(x => x.Key == "ShortName").Value.Value as string;

                bool isRequired = (bool?)optAttr.NamedArguments
                    .FirstOrDefault(x => x.Key == "IsRequired").Value.Value ?? false;

                options.Add(new OptionInfo(longName, shortName, prop.Name, isRequired, prop.Type));
            }

            result.Add(new CommandInfo(
                commandName ?? type.Name.ToLowerInvariant(),
                description ?? "",
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                options.ToImmutable()));
        }

        return result;
    }

    // ------------------------------------------------------------------
    //  Descriptors
    // ------------------------------------------------------------------

    private static void EmitDescriptors(SourceProductionContext spc, List<CommandInfo> commands)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using AttributesTestApp.Gen;");
        sb.AppendLine();
        sb.AppendLine("namespace AttributesTestApp.Gen");
        sb.AppendLine("{");
        sb.AppendLine("    public static class CommandDescriptorsStorage");
        sb.AppendLine("    {");
        sb.AppendLine("        public static Dictionary<string, CommandDescriptor> Commands = new()");
        sb.AppendLine("        {");

        foreach (var cmd in commands)
        {
            sb.Append("            [\"").Append(cmd.Name).Append("\"] = new CommandDescriptor()");
            sb.AppendLine();
            sb.AppendLine("            {");
            sb.Append("                Name = \"").Append(Escape(cmd.Name)).AppendLine("\",");
            sb.Append("                Description = \"").Append(Escape(cmd.Description)).AppendLine("\",");
            sb.Append("                Type = typeof(").Append(cmd.TypeRef).AppendLine("),");
            sb.AppendLine("                Options = new OptionDescriptor[]");
            sb.AppendLine("                {");

            foreach (var opt in cmd.Options)
            {
                string propType = opt.PropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string shortNameLiteral = opt.ShortName is null
                    ? "null"
                    : $"\"{Escape(opt.ShortName)}\"";

                sb.Append("                    new OptionDescriptor() { LongName = \"")
                  .Append(Escape(opt.LongName)).Append("\", ShortName = ")
                  .Append(shortNameLiteral)
                  .Append(", IsRequired = ").Append(opt.IsRequired ? "true" : "false")
                  .Append(", PropertyType = typeof(").Append(propType).Append(")")
                  .Append(", PropertyName = \"").Append(Escape(opt.PropertyName)).Append("\"")
                  .AppendLine(" },");
            }

            sb.AppendLine("                },");
            sb.AppendLine("            },");
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        spc.AddSource("CommandDescriptorsStorage.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    // ------------------------------------------------------------------
    //  Binder
    // ------------------------------------------------------------------

    private static void EmitBinder(SourceProductionContext spc, List<CommandInfo> commands)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Globalization;");
        sb.AppendLine("using AttributesTestApp.Gen;");
        sb.AppendLine();
        sb.AppendLine("namespace AttributesTestApp.Gen");
        sb.AppendLine("{");
        sb.AppendLine("    public static class CommandArgumentBinder");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void Bind(object command, CommandDescriptor descriptor, string[] args)");
        sb.AppendLine("        {");
        sb.AppendLine("            switch (descriptor.Name)");
        sb.AppendLine("            {");

        for (int idx = 0; idx < commands.Count; idx++)
        {
            var cmd = commands[idx];
            sb.Append("                case \"").Append(Escape(cmd.Name)).AppendLine("\":");
            sb.Append("                    Bind_").Append(MethodSuffix(cmd.Name, idx))
              .Append("((").Append(cmd.TypeRef).AppendLine(")command, args);");
            sb.AppendLine("                    break;");
        }

        sb.AppendLine("                default:");
        sb.AppendLine("                    throw new InvalidOperationException($\"No binder for '{descriptor.Name}'\");");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();

        for (int idx = 0; idx < commands.Count; idx++)
        {
            var cmd = commands[idx];
            sb.Append("        private static void Bind_").Append(MethodSuffix(cmd.Name, idx))
              .Append('(').Append(cmd.TypeRef).AppendLine(" cmd, string[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("            for (int i = 0; i < args.Length; i++)");
            sb.AppendLine("            {");
            sb.AppendLine("                switch (args[i])");
            sb.AppendLine("                {");

            foreach (var opt in cmd.Options)
            {
                sb.Append("                    case \"--").Append(Escape(opt.LongName)).AppendLine("\":");
                if (opt.ShortName is not null)
                    sb.Append("                    case \"-").Append(Escape(opt.ShortName)).AppendLine("\":");

                var (parseExpr, takesValue) = GetParseLogic(opt.PropertyType);

                if (takesValue)
                {
                    sb.AppendLine("                        if (i + 1 >= args.Length)");
                    sb.Append("                            throw new ArgumentException($\"")
                      .Append(Escape(opt.LongName)).AppendLine(" requires a value\");");
                }

                sb.Append("                        cmd.").Append(opt.PropertyName)
                  .Append(" = ").Append(parseExpr).AppendLine(";");
                sb.AppendLine("                        break;");
            }

            sb.AppendLine("                    default:");
            sb.AppendLine("                        throw new ArgumentException($\"Unknown option: {args[i]}\");");
            sb.AppendLine("                }");
            sb.AppendLine("            }");

            // Required-option validation
            var required = cmd.Options.Where(o => o.IsRequired).ToList();
            if (required.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("            // required-option validation");
                foreach (var opt in required)
                {
                    if (opt.PropertyType.SpecialType == SpecialType.System_String)
                    {
                        sb.Append("            if (string.IsNullOrEmpty(cmd.").Append(opt.PropertyName)
                          .AppendLine("))");
                        sb.Append("                throw new ArgumentException(\"Required option missing: ")
                          .Append(Escape(opt.LongName)).AppendLine("\");");
                    }
                    else if (opt.PropertyType.IsValueType
                        && opt.PropertyType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                    {
                        // value types: compare against default
                        string fullType = opt.PropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        sb.Append("            if (EqualityComparer<").Append(fullType)
                          .Append(">.Default.Equals(cmd.").Append(opt.PropertyName)
                          .AppendLine(", default))");
                        sb.Append("                throw new ArgumentException(\"Required option missing: ")
                          .Append(Escape(opt.LongName)).AppendLine("\");");
                    }
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        spc.AddSource("CommandArgumentBinder.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    // ------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------

    private static (string parseExpr, bool takesValue) GetParseLogic(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum)
        {
            string enumRef = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return ($"Enum.Parse<{enumRef}>(args[++i], ignoreCase: true)", true);
        }

        return type.SpecialType switch
        {
            SpecialType.System_Boolean => ("true", false),
            SpecialType.System_String => ("args[++i]", true),
            SpecialType.System_Int32 => ("int.Parse(args[++i], CultureInfo.InvariantCulture)", true),
            SpecialType.System_Int64 => ("long.Parse(args[++i], CultureInfo.InvariantCulture)", true),
            SpecialType.System_Double => ("double.Parse(args[++i], CultureInfo.InvariantCulture)", true),
            SpecialType.System_Decimal => ("decimal.Parse(args[++i], CultureInfo.InvariantCulture)", true),
            _ => ("args[++i]", true),
        };
    }

    private static string MethodSuffix(string name, int index)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        if (sb.Length == 0 || char.IsDigit(sb[0]))
            sb.Insert(0, '_');
        sb.Append('_').Append(index);
        return sb.ToString();
    }

    private static string Escape(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static bool ContainsErrors(CSharpSyntaxNode node)
        => node.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);
}