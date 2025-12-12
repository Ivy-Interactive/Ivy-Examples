namespace SnowflakeDashboard.Helpers;

public class CodeView(Type type) : ViewBase
{
    public override object? Build()
    {
        var assembly = typeof(CodeView).Assembly;
        var resourceName = "SnowflakeDashboard.Apps.DashboardApp.cs";
        string code;
        using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                return new Exception("Resource not found.");
            }
            using (StreamReader reader = new StreamReader(stream))
            {
                code = reader.ReadToEnd();
            }
        }
        return new Code(code, Languages.Csharp).Width(Size.Fit()).Height(Size.Fit());
    }
}

