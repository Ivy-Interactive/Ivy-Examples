#:package Ivy@1.2.27
#:package Ivy.Analyser@1.2.27

global using Ivy;
global using System.Globalization;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();
#if DEBUG
server.UseHotReload();
#endif

server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

var appShellSettings = new AppShellSettings()
    .DefaultApp<HelloApp>()
    .UseTabs(preventDuplicates: true);

server.UseAppShell(appShellSettings);
await server.RunAsync();


[App(icon: Icons.PartyPopper, title: "Hello")]
public class HelloApp : ViewBase
{
    public override object? Build()
    {
        var nameState = this.UseState<string>();

        return Layout.Center()
               | (new Card(
                   Layout.Vertical().Gap(6).Padding(2)
                   | new Confetti(new IvyLogo())
                   | Text.H2("Hello " + (string.IsNullOrEmpty(nameState.Value) ? "there" : nameState.Value) + "!")
                   | Text.Block("Welcome to the fantastic world of Ivy. Let's build something amazing together!")
                   | nameState.ToInput(placeholder: "What is your name?")
                   | new Separator()
                   | Text.Markdown("You'd be a hero to us if you could ⭐ us on [Github](https://github.com/Ivy-Interactive/Ivy-Framework)")
                 )
                 .Width(Size.Units(120).Max(500)));
    }
}

