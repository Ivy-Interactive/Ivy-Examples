namespace CourseTemplate.Apps.Services;

internal class GenerationService(FileSystemService fs)
{
    private readonly FileSystemService _fs = fs;

    public void RegenerateSingleBlocking(string mdFullPath)
    {
        var root = _fs.FindProjectRootFromPath(mdFullPath);
        var generated = System.IO.Path.Combine(root, "Generated");
        var projectFile = System.IO.Path.Combine(root, "course-template.csproj");
        var generatorProj = System.IO.Path.Combine(root, "Helpers", "Generator", "Generator.csproj");
        var expectedGenerated = _fs.GetExpectedGeneratedPath(mdFullPath, root, generated);
        var startTs = DateTime.UtcNow;

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{generatorProj}\" -- convert \"{mdFullPath}\" \"{generated}\" \"{projectFile}\"",
            WorkingDirectory = root,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            var proc = System.Diagnostics.Process.Start(psi);
            proc?.WaitForExit();

            for (int i = 0; i < 200; i++)
            {
                if (System.IO.File.Exists(expectedGenerated))
                {
                    var wt = System.IO.File.GetLastWriteTimeUtc(expectedGenerated);
                    if (wt >= startTs)
                        break;
                }
                System.Threading.Thread.Sleep(100);
            }
        }
        catch
        {
            // swallow errors for now; upstream handles UX state
        }
    }
}


