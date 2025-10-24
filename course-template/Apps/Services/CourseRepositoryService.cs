using System.Text.RegularExpressions;

namespace CourseTemplate.Apps.Services;

internal class CourseRepositoryService(FileSystemService fs)
{
    private readonly FileSystemService _fs = fs;

    public string? GetModulesRoot()
    {
        return _fs.FindModulesFolder();
    }

    public (bool ok, string message, string? createdPath) CreateFolder(string parentFullPath, string displayName)
    {
        var modulesRoot = _fs.FindModulesFolder();
        if (modulesRoot == null)
            return (false, "Modules folder not found", null);

        if (!IsPathUnder(modulesRoot, parentFullPath))
            return (false, "Parent folder is invalid or outside of Modules", null);
        if (!Directory.Exists(parentFullPath))
        {
            try { Directory.CreateDirectory(parentFullPath); }
            catch (Exception ex) { return (false, ex.Message, null); }
        }

        var safeName = SanitizeName(displayName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "Item";
        if (char.IsDigit(safeName[0]))
            safeName = "_" + safeName;

        var (nextOrder, digits) = GetNextOrder(parentFullPath);
        var nameWithOrder = FormatWithOrder(safeName, nextOrder, digits);
        var folderPath = Path.Combine(parentFullPath, nameWithOrder);

        while (Directory.Exists(folderPath) || File.Exists(folderPath))
        {
            nextOrder++;
            nameWithOrder = FormatWithOrder(safeName, nextOrder, digits);
            folderPath = Path.Combine(parentFullPath, nameWithOrder);
        }

        try
        {
            Directory.CreateDirectory(folderPath);

            // create default _Index.md
            var indexPath = Path.Combine(folderPath, "_Index.md");
            if (!File.Exists(indexPath))
            {
                var title = FileSystemService.RemoveOrderPrefix(nameWithOrder);
                System.IO.File.WriteAllText(indexPath, $"---\ngroupExpanded: true\n---\n");
            }

            return (true, "Folder created", folderPath);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public (bool ok, string message, string? createdPath) CreatePage(string parentFullPath, string displayName)
    {
        var modulesRoot = _fs.FindModulesFolder();
        if (modulesRoot == null)
            return (false, "Modules folder not found", null);

        if (!IsPathUnder(modulesRoot, parentFullPath) || !Directory.Exists(parentFullPath))
            return (false, "Parent folder is invalid or outside of Modules", null);

        var safeName = SanitizeName(displayName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "Page";
        if (char.IsDigit(safeName[0]))
            safeName = "_" + safeName;

        var (nextOrder, digits) = GetNextOrder(parentFullPath);
        var fileName = FormatWithOrder(safeName, nextOrder, digits) + ".md";
        var filePath = Path.Combine(parentFullPath, fileName);

        while (File.Exists(filePath) || Directory.Exists(filePath))
        {
            nextOrder++;
            fileName = FormatWithOrder(safeName, nextOrder, digits) + ".md";
            filePath = Path.Combine(parentFullPath, fileName);
        }

        try
        {
            var title = safeName;
            using (var fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var sw = new StreamWriter(fs))
            {
                sw.Write($"# {title}\n\n");
            }
            return (true, "Page created", filePath);
        }
        catch (IOException)
        {
            // If file exists, still consider success and return path
            if (System.IO.File.Exists(filePath)) return (true, "Page already exists", filePath);
            throw;
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public (bool ok, string message) DeletePath(string fullPath)
    {
        var modulesRoot = _fs.FindModulesFolder();
        if (modulesRoot == null)
            return (false, "Modules folder not found");

        if (!IsPathUnder(modulesRoot, fullPath))
            return (false, "Target path is outside of Modules");

        try
        {
            if (Directory.Exists(fullPath))
            {
                if (PathsEqual(modulesRoot, fullPath))
                    return (false, "Cannot delete Modules root");
                
                // Удаляем папку в Modules
                Directory.Delete(fullPath, recursive: true);
                
                // Удаляем всю папку Generated для полной перегенерации
                var projectRoot = _fs.FindProjectRootFromPath(fullPath);
                var generatedRoot = System.IO.Path.Combine(projectRoot, "Generated");
                
                if (System.IO.Directory.Exists(generatedRoot))
                {
                    System.IO.Directory.Delete(generatedRoot, recursive: true);
                }
                
                return (true, "Folder deleted");
            }

            if (File.Exists(fullPath))
            {
                // Удаляем .md файл
                File.Delete(fullPath);
                
                // Удаляем соответствующий .g.cs файл, если он существует
                var projectRoot = _fs.FindProjectRootFromPath(fullPath);
                var generatedRoot = System.IO.Path.Combine(projectRoot, "Generated");
                var expectedGeneratedPath = _fs.GetExpectedGeneratedPath(fullPath, projectRoot, generatedRoot);
                
                if (System.IO.File.Exists(expectedGeneratedPath))
                {
                    System.IO.File.Delete(expectedGeneratedPath);
                }
                
                return (true, "File deleted");
            }

            return (false, "Path does not exist");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static (int nextOrder, int digits) GetNextOrder(string parentFullPath)
    {
        var max = 0;
        var maxLen = 2;
        foreach (var entry in Directory.EnumerateFileSystemEntries(parentFullPath))
        {
            var name = Path.GetFileName(entry);
            if (string.IsNullOrEmpty(name) || name.StartsWith("_"))
                continue; // skip special files like _Index.md

            var m = Regex.Match(name, "^(\\d+)_");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
            {
                if (n > max) max = n;
                if (m.Groups[1].Value.Length > maxLen) maxLen = m.Groups[1].Value.Length;
            }
        }
        return (max + 1, Math.Max(2, maxLen));
    }

    private static string FormatWithOrder(string name, int order, int digits)
    {
        return order.ToString("D" + digits) + "_" + name;
    }

    private static string SanitizeName(string input)
    {
        var trimmed = input.Trim();
        trimmed = Regex.Replace(trimmed, "[^A-Za-z0-9 _-]+", "");
        trimmed = Regex.Replace(trimmed, "[\u00A0\\s]+", " "); // collapse whitespace
        trimmed = trimmed.Replace(' ', '_');
        trimmed = Regex.Replace(trimmed, "_+", "_");
        return trimmed.Trim('_');
    }

    private static bool IsPathUnder(string root, string candidate)
    {
        var fullRoot = Path.GetFullPath(root);
        var fullCand = Path.GetFullPath(candidate);
        if (!fullRoot.EndsWith(Path.DirectorySeparatorChar))
            fullRoot += Path.DirectorySeparatorChar;
        return fullCand.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathsEqual(string a, string b)
    {
        return string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}


