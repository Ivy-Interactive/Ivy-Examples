namespace DiffengineExample.Services;

public static class DiffService
{
    public static bool IsCi => BuildServerDetector.Detected;

    public static async Task<string> SaveTextToTempAsync(string text, string extNoDot)
    {
        var path = Path.ChangeExtension(Path.GetTempFileName(), "." + extNoDot.TrimStart('.'));
        await File.WriteAllTextAsync(path, text ?? string.Empty);
        return path;
    }

    public static async Task<string> SaveBytesToTempAsync(byte[] bytes, string extNoDot)
    {
        var path = Path.ChangeExtension(Path.GetTempFileName(), "." + extNoDot.TrimStart('.'));
        await File.WriteAllBytesAsync(path, bytes ?? Array.Empty<byte>());
        return path;
    }

    public static async Task<(string left, string right)> LaunchTextAsync(string leftText, string rightText, string extNoDot)
    {
        var left = await SaveTextToTempAsync(leftText, extNoDot);
        var right = await SaveTextToTempAsync(rightText, extNoDot);
        await DiffRunner.LaunchAsync(left, right);
        return (left, right);
    }

    public static async Task<(string left, string right)> LaunchFilesAsync(string leftFilePath, string rightFilePath, string extNoDot)
    {
        var leftBytes = await File.ReadAllBytesAsync(leftFilePath);
        var rightBytes = await File.ReadAllBytesAsync(rightFilePath);

        var left = await SaveBytesToTempAsync(leftBytes, extNoDot);
        var right = await SaveBytesToTempAsync(rightBytes, extNoDot);
        await DiffRunner.LaunchAsync(left, right);
        return (left, right);
    }

    public static void Kill(string left, string right)
    {
        try
        {
            // Ask DiffEngine first (targeted close)
            DiffRunner.Kill(left, right);
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"Exception in DiffRunner.Kill: {ex}");
#else
    Console.Error.WriteLine($"Exception in DiffRunner.Kill: {ex.Message}");
#endif
        }

        // Fallback for WinMerge windows that may remain
        try
        {
            foreach (var name in new[] { "WinMergeU", "WinMerge" })
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    if (p.HasExited) continue;
                    if (!p.CloseMainWindow())
                        p.Kill(entireProcessTree: true);
                }
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"Error while attempting to close WinMerge processes: {ex}");
#else
    Console.Error.WriteLine($"Error while attempting to close WinMerge processes: {ex.Message}");
#endif
        }
    }
}