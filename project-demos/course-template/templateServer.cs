namespace CourseTemplate;

public static class TemplateServer
{
    public static async Task RunAsync(ServerArgs? args = null)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
        var server = new Server(args);
        server.AddAppsFromAssembly(typeof(TemplateServer).Assembly);
        server.UseHotReload();

        var version = typeof(Server).Assembly.GetName().Version!.ToString().EatRight(".0");
        server.SetMetaTitle($"Ivy Course Template {version}");

        var chromeSettings = new ChromeSettings()
            .Header(
                Layout.Vertical().Padding(2)
                | new IvyLogo()
                | Text.Muted($"Version {version}")
            )
            .DefaultApp<Apps.Module1.Theme1.Section1.Paragraph1App>()
            .UsePages();
        server.UseChrome(() => new DefaultSidebarChrome(chromeSettings));

        await server.RunAsync();
    }
}