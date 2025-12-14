namespace SnowflakeDashboard.Helpers;

public class CodeView : ViewBase
{
    public override object? Build()
    {
        var assembly = typeof(CodeView).Assembly;
        var resourceName = "SnowflakeDashboard.Apps.DashboardApp.cs";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return new Exception("Resource not found.");
        }
        
        using var reader = new StreamReader(stream);
        var code = reader.ReadToEnd();
        
        return new Code(code, Languages.Csharp).Width(Size.Fit()).Height(Size.Fit());
    }
}

