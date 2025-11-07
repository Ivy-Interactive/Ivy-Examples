using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 3 || args[0] != "convert")
        {
            Console.WriteLine("Usage: Generator convert <inputFolder> <outputFolder> [projectFile]");
            return 1;
        }

        string inputPattern = args[1];
        string outputFolder = args[2];
        string? projectFile = args.Length > 3 ? args[3] : null;

        var pattern = Path.GetFileName(inputPattern);
        var inputFolder = Path.GetFullPath(Path.GetDirectoryName(inputPattern)!);
        outputFolder = Path.GetFullPath(outputFolder);

        Directory.CreateDirectory(outputFolder);

        // Find project file if not provided
        projectFile ??= FindProjectFile(inputFolder);
        var rootNamespace = GetRootNamespace(projectFile);

        var tasks = Directory.GetFiles(inputFolder, pattern, SearchOption.AllDirectories).Select(async absoluteInputPath =>
        {
            var (order, name) = GetOrderFromFileName(absoluteInputPath);

            if (name == "_Index")
            {
                (order, _) = GetOrderFromFileName(Path.GetFileName(Path.GetDirectoryName(absoluteInputPath))!);
            }

            string relativeInputPath = Path.GetRelativePath(inputFolder, absoluteInputPath);
            string relativeOutputPath = GetRelativeFolderWithoutOrder(inputFolder, absoluteInputPath);
            string folder = Path.GetFullPath(Path.Combine(outputFolder, relativeOutputPath));

            Directory.CreateDirectory(folder);

            string ivyOutput = Path.Combine(folder, $"{name}.g.cs");
            var namespaceSuffix = relativeOutputPath
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(Path.AltDirectorySeparatorChar, '.').Trim('.');

            if (namespaceSuffix.StartsWith("Generated."))
                namespaceSuffix = namespaceSuffix.Substring("Generated.".Length);

            string @namespace = string.IsNullOrEmpty(namespaceSuffix)
                ? $"{rootNamespace}.Apps"
                : $"{rootNamespace}.Apps.{namespaceSuffix}";

            await ConvertMarkdownAsync(name, relativeInputPath, absoluteInputPath, ivyOutput, @namespace, order);
        });

        await Task.WhenAll(tasks);
        Console.WriteLine($"✅ Generated {tasks.Count()} files successfully!");
        return 0;
    }

    static string GetRootNamespace(string projectFile)
    {
        var doc = XDocument.Load(projectFile);
        var rootNamespace = doc.Descendants("RootNamespace").FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(rootNamespace))
            throw new Exception("No <RootNamespace> element found in the project file.");
        return rootNamespace;
    }

    static string FindProjectFile(string startFolder)
    {
        var currentFolder = startFolder;
        while (!string.IsNullOrEmpty(currentFolder))
        {
            var csprojFiles = Directory.GetFiles(currentFolder, "*.csproj");
            if (csprojFiles.Length > 0)
                return csprojFiles[0];
            currentFolder = Directory.GetParent(currentFolder)?.FullName;
        }
        throw new FileNotFoundException("No .csproj file found in the directory hierarchy.");
    }

    static (int? order, string name) GetOrderFromFileName(string filename)
    {
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        var parts = nameWithoutExtension.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[0], out int order))
        {
            return (order, string.Join("_", parts.Skip(1)));
        }
        return (null, nameWithoutExtension);
    }

    static string GetRelativeFolderWithoutOrder(string inputFolder, string inputFile)
    {
        var fileDirectory = Path.GetDirectoryName(inputFile)!;
        var relativePath = Path.GetRelativePath(inputFolder, fileDirectory);
        var parts = relativePath
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => Regex.Replace(p, @"^\d+_", ""))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        var result = Path.Combine(parts);
        if (result == "." || string.IsNullOrEmpty(result))
            return "";

        return result.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    static async Task ConvertMarkdownAsync(string name, string relativePath, string absolutePath, string outputFile, 
        string @namespace, int? order)
    {
        string className = name + "App";
        string markdownContent = await File.ReadAllTextAsync(absolutePath);

        Console.WriteLine($"Converting {relativePath} to {Path.GetFileName(outputFile)}");

        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePreciseSourceLocation()
            .UseYamlFrontMatter()
            .Build();

        var document = Markdown.Parse(markdownContent, pipeline);

        var appMeta = new AppMeta();
        var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
        if (yamlBlock != null)
        {
            string yamlContent = markdownContent.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);
            appMeta = ParseYamlAppMeta(yamlContent);
        }

        if (order != null)
            appMeta.Order = order.Value;

        var codeBuilder = new StringBuilder();
        codeBuilder.AppendLine("using Ivy;");
        codeBuilder.AppendLine("using Ivy.Apps;");
        codeBuilder.AppendLine("using Ivy.Shared;");
        codeBuilder.AppendLine("using Ivy.Core;");
        codeBuilder.AppendLine("using static Ivy.Views.Layout;");
        codeBuilder.AppendLine("using static Ivy.Views.Text;");
        codeBuilder.AppendLine();
        codeBuilder.AppendLine($"namespace {@namespace};");
        codeBuilder.AppendLine();
        codeBuilder.Append($"[App(order:{appMeta.Order}");
        codeBuilder.Append(appMeta.Icon != null ? $", icon:Icons.{appMeta.Icon}" : "");
        codeBuilder.Append(appMeta.Title != null ? $", title:{FormatLiteral(appMeta.Title)}" : "");
        codeBuilder.Append(appMeta.GroupExpanded ? ", groupExpanded:true" : "");
        codeBuilder.AppendLine(")]");
        codeBuilder.AppendLine($"public class {className}() : {appMeta.ViewBase}");
        codeBuilder.AppendLine("{");
        codeBuilder.AppendTab(1).AppendLine("public override object? Build()");
        codeBuilder.AppendTab(1).AppendLine("{");

        if (document.Any(e => e is not YamlFrontMatterBlock))
        {
            codeBuilder.AppendTab(2).AppendLine("var appDescriptor = this.UseService<AppDescriptor>();");
            codeBuilder.AppendTab(2).AppendLine("var onLinkClick = this.UseLinks();");
            codeBuilder.AppendTab(2).AppendLine("var article = new Article().ShowToc(true).ShowFooter(true)");

            // Simplified markdown handling - just render everything as markdown
            var contentBuilder = new StringBuilder();
            foreach (var block in document)
            {
                if (block is not YamlFrontMatterBlock)
                {
                    string blockContent = markdownContent.Substring(block.Span.Start, block.Span.Length).Trim();
                    contentBuilder.AppendLine(blockContent);
                }
            }

            var content = contentBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(content))
            {
                AppendAsMultiLineString(3, content, codeBuilder, "| new Markdown(", ").HandleLinkClick(onLinkClick)");
            }

            codeBuilder.AppendTab(3).AppendLine(";");
            codeBuilder.AppendTab(2).AppendLine("return article;");
        }
        else
        {
            codeBuilder.AppendTab(2).AppendLine("return null;");
        }

        codeBuilder.AppendTab(1).AppendLine("}");
        codeBuilder.AppendLine("}");

        await File.WriteAllTextAsync(outputFile, codeBuilder.ToString());
    }

    static AppMeta ParseYamlAppMeta(string yaml)
    {
        string withoutDashes = string.Join(Environment.NewLine,
            yaml.Split('\n').Skip(1).SkipLast(1).Select(e => e.TrimEnd('\r')));

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<AppMeta>(withoutDashes);
    }

    static void AppendAsMultiLineString(int tabs, string content, StringBuilder sb, string prepend, string append)
    {
        if (content.Contains('\n') || content.Contains('"'))
        {
            var lines = content.Split('\n');
            sb.AppendTab(tabs).AppendLine(prepend);
            sb.AppendTab(tabs + 1).AppendLine("\"\"\"\"");
            foreach (var line in lines)
            {
                sb.AppendTab(tabs + 1).AppendLine(line.TrimEnd());
            }
            sb.AppendTab(tabs + 1).AppendLine($"\"\"\"\"{append}");
        }
        else
        {
            sb.AppendTab(tabs).AppendLine($"{prepend}{FormatLiteral(content)}{append}");
        }
    }

    static string FormatLiteral(string literal) => SymbolDisplay.FormatLiteral(literal, true);
}

class AppMeta
{
    public string? Icon { get; set; }
    public int Order { get; set; } = 0;
    public string? Title { get; set; }
    public string ViewBase { get; set; } = "ViewBase";
    public bool GroupExpanded { get; set; } = false;
}

static class StringBuilderExtensions
{
    public static StringBuilder AppendTab(this StringBuilder sb, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            sb.Append("    ");
        }
        return sb;
    }
}

