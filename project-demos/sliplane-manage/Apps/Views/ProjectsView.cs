namespace SliplaneManage.Apps.Views;

using SliplaneManage.Models;
using SliplaneManage.Services;

/// <summary>
/// Full projects management view: list, create, rename, delete.
/// </summary>
public class ProjectsView : ViewBase
{
    private readonly string _apiToken;

    public ProjectsView(string apiToken)
    {
        _apiToken = apiToken;
    }

    public override object? Build()
    {
        var client   = this.UseService<SliplaneApiClient>();
        var projects = this.UseState<List<SliplaneProject>>();
        var loading  = this.UseState(true);
        var error    = this.UseState<string?>();
        var creating = this.UseState(false);
        var newName  = this.UseState(string.Empty);
        var busy     = this.UseState(false);

        var refresh = this.UseRefreshToken();

        async Task LoadProjectsAsync()
        {
            try
            {
                var list = await client.GetProjectsAsync(_apiToken);
                projects.Set(list);
            }
            catch (Exception ex)
            {
                error.Set(ex.Message);
            }
            finally
            {
                loading.Set(false);
            }
        }

        this.UseEffect(async () => await LoadProjectsAsync());

        if (loading.Value)
            return Layout.Center() | Text.Muted("Loading projects...");

        if (error.Value is { Length: > 0 })
            return new Callout($"Error: {error.Value}", variant: CalloutVariant.Error);

        async ValueTask CreateProject()
        {
            if (string.IsNullOrWhiteSpace(newName.Value)) return;
            busy.Set(true);
            await client.CreateProjectAsync(_apiToken, newName.Value.Trim());
            newName.Set(string.Empty);
            creating.Set(false);
            busy.Set(false);
            refresh.Refresh();
        }

        async ValueTask DeleteProject(string id)
        {
            busy.Set(true);
            await client.DeleteProjectAsync(_apiToken, id);
            busy.Set(false);
            refresh.Refresh();
        }

        return Layout.Vertical()
            | (Layout.Horizontal().Align(Align.Center)
               | Text.H2("Projects")
               | new Button("New Project").Icon(Icons.Plus).Variant(ButtonVariant.Outline)
                   .HandleClick(() => creating.Set(!creating.Value)))
            | (creating.Value
                ? (object)(new Card(
                    Layout.Vertical()
                    | Text.H4("Create Project")
                    | newName.ToTextInput().Placeholder("Project name")
                    | (Layout.Horizontal().Align(Align.Right)
                       | new Button("Cancel").Variant(ButtonVariant.Ghost).HandleClick(() => creating.Set(false))
                       | new Button("Create").Icon(Icons.Plus).Loading(busy.Value).HandleClick(async () => await CreateProject())))
                  ).Width(Size.Fraction(0.45f))
                : Layout.Vertical())
            | BuildProjectTable(projects.Value ?? new List<SliplaneProject>(), DeleteProject);
    }

    private static object BuildProjectTable(
        List<SliplaneProject> projects,
        Func<string, ValueTask> onDelete)
    {
        if (projects.Count == 0)
            return new Callout("No projects yet. Create one above.", variant: CalloutVariant.Info);

        var rows = projects
            .Select(p =>
                Layout.Horizontal().Gap(2)
                | Text.InlineCode(p.Id)
                | Text.Block(p.Name)
                | new Spacer()
                | new Button("Delete")
                    .Icon(Icons.Trash2)
                    .Variant(ButtonVariant.Outline)
                    .HandleClick(async () => await onDelete(p.Id))
            )
            .ToArray();

        return Layout.Vertical() | rows;
    }
}
