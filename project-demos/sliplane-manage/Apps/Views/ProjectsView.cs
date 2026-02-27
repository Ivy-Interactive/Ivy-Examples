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
        var serviceCounts = this.UseState<Dictionary<string, int>>(() => new Dictionary<string, int>());
        var (sheetView, showSheet) = this.UseTrigger(
            (IState<bool> isOpen, SliplaneProject project) => new ProjectDetailsSheet(isOpen, _apiToken, project));

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

        // Preload service counts per project so they are visible on cards
        this.UseEffect(async () =>
        {
            var current = projects.Value;
            if (current == null || current.Count == 0) return;

            var map = new Dictionary<string, int>();

            foreach (var p in current)
            {
                try
                {
                    var svcs = await client.GetServicesAsync(_apiToken, p.Id);
                    map[p.Id] = svcs?.Count ?? 0;
                }
                catch
                {
                    map[p.Id] = 0;
                }
            }

            serviceCounts.Set(map);
        }, [projects]);

        if (loading.Value)
            return Layout.Center() | Text.Muted("Loading projects...");

        if (error.Value is { Length: > 0 })
            return new Callout($"Error: {error.Value}", variant: CalloutVariant.Error);

        var list = projects.Value ?? new List<SliplaneProject>();

        object projectsBlock;
        if (list.Count == 0)
        {
            projectsBlock = new Callout("No projects yet. Create one above.", variant: CalloutVariant.Info);
        }
        else
        {
            var cards = list
                .Select(p =>
                {
                    var hasCount = serviceCounts.Value.TryGetValue(p.Id, out var svcCount);
                    var label = hasCount
                        ? $"{svcCount} Service" + (svcCount == 1 ? string.Empty : "s")
                        : "Services: —";

                    var icon = hasCount && svcCount > 0
                        ? Icons.FolderOpen
                        : Icons.Folder;

                    return new Card(
                            (Layout.Vertical().Align(Align.Center)
                            | Text.H2(p.Name)
                            | Text.Muted(label))
                        )
                        .Title("Project")
                        .Icon(icon)
                        .HandleClick(_ => showSheet(p));
                })
                .ToArray();

            projectsBlock = Layout.Grid().Columns(3).Gap(3) | cards;
        }


        return new Fragment(
            Layout.Vertical()
                | Text.H2("Projects")
                | projectsBlock,
            sheetView
        );
    }
}

internal sealed class ProjectDetailsSheet : ViewBase
{
    private readonly IState<bool> _isOpen;
    private readonly string _apiToken;
    private readonly SliplaneProject _project;

    public ProjectDetailsSheet(IState<bool> isOpen, string apiToken, SliplaneProject project)
    {
        _isOpen = isOpen;
        _apiToken = apiToken;
        _project = project;
    }

    public override object? Build()
    {
        if (!_isOpen.Value)
        {
            return null;
        }

        var client = this.UseService<SliplaneApiClient>();
        var services = this.UseState<List<SliplaneService>?>(() => null);
        var loading = this.UseState(true);
        var error = this.UseState<string?>();

        this.UseEffect(async () =>
        {
            try
            {
                var list = await client.GetServicesAsync(_apiToken, _project.Id);
                services.Set(list);
            }
            catch (Exception ex)
            {
                error.Set(ex.Message);
            }
            finally
            {
                loading.Set(false);
            }
        });

        object body;
        if (loading.Value)
        {
            body = Text.Muted("Loading project services...");
        }
        else if (error.Value is { Length: > 0 })
        {
            body = new Callout($"Error loading services: {error.Value}", variant: CalloutVariant.Error);
        }
        else if (services.Value == null || services.Value.Count == 0)
        {
            body = new Callout("No services in this project yet.", variant: CalloutVariant.Info);
        }
        else
        {
            var rows = services.Value
                .Select(s =>
                    Layout.Horizontal().Gap(2)
                    | new Badge(s.Status ?? "—")
                    | Text.Block(s.Name)
                )
                .ToArray();

            body = Layout.Vertical().Gap(2) | rows;
        }

        var content = Layout.Vertical().Gap(3)
            | Text.H3(_project.Name)
            | Text.InlineCode(_project.Id)
            | body;

        return new Sheet(
            onClose: _ => { _isOpen.Set(false); return ValueTask.CompletedTask; },
            content: new Card(Layout.Vertical().Padding(4) | content),
            title: $"Project: {_project.Name}"
        );
    }
}
