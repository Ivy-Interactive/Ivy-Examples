using System.Text.RegularExpressions;

namespace CourseTemplate.Apps.Services;

internal class FileSystemService
{
    public string? FindModulesFolder()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 5; i++)
        {
            var candidate = System.IO.Path.Combine(dir, "Modules");
            if (System.IO.Directory.Exists(candidate))
                return candidate;
            var parent = System.IO.Directory.GetParent(dir)?.FullName;
            if (parent == null) break;
            dir = parent;
        }
        return null;
    }

    public string FindProjectRootFromPath(string filePath)
    {
        var dir = System.IO.Path.GetDirectoryName(filePath)!;
        while (dir != null)
        {
            var csproj = System.IO.Path.Combine(dir, "course-template.csproj");
            if (System.IO.File.Exists(csproj)) return dir;
            var parent = System.IO.Directory.GetParent(dir)?.FullName;
            if (parent == null) break;
            dir = parent;
        }
        return AppContext.BaseDirectory;
    }

    public string GetExpectedGeneratedPath(string mdPath, string projectRoot, string generatedRoot)
    {
        var modulesRoot = System.IO.Path.Combine(projectRoot, "Modules");
        var relative = System.IO.Path.GetRelativePath(modulesRoot, mdPath);
        var parts = relative
            .Split(new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
            .Select(RemoveOrderPrefix)
            .ToArray();
        var fileWithoutExt = System.IO.Path.GetFileNameWithoutExtension(parts[^1]);
        parts[^1] = fileWithoutExt + ".g.cs";
        return parts.Aggregate(generatedRoot, System.IO.Path.Combine);
    }

    public static string RemoveOrderPrefix(string segment)
    {
        return Regex.Replace(segment, "^\\d+_", "");
    }
}


