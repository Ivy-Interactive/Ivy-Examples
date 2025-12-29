#:package Ivy@1.2.6
#:package Microsoft.Data.Sqlite@8.0.0

global using Ivy;
global using Ivy.Apps;
global using Ivy.Chrome;
global using Ivy.Client;
global using Ivy.Core;
global using Ivy.Core.Hooks;
global using Ivy.Hooks;
global using Ivy.Services;
global using Ivy.Shared;
global using Ivy.Views;
global using Ivy.Views.Alerts;
global using Ivy.Views.Blades;
global using Ivy.Views.Builders;
global using Ivy.Views.Forms;
global using Ivy.Views.Charts;
global using Ivy.Widgets.Inputs;
global using System.Globalization;
global using System.ComponentModel.DataAnnotations;
global using Microsoft.Data.Sqlite;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();
#if DEBUG
server.UseHotReload();
#endif

server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

var customHeader = Layout.Vertical().Gap(2)
    |new Embed("https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=Ivy-Interactive%2FIvy-Examples&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fcrm-filebased%2Fdevcontainer.json&location=EuropeWest");
var chromeSettings = new ChromeSettings()
    .DefaultApp<DashboardApp>()
    .UseTabs(preventDuplicates: true)
    .Header(customHeader);
server.UseChrome(chromeSettings);
server.UseVolume(new FolderVolume(Ivy.Utils.IsProduction() ? "/app/data" : null));
await server.RunAsync();

// Models
record TaskItem(Guid Id, string Title, bool IsCompleted, DateTime CreatedAt);
record NoteItem(Guid Id, string Title, string Content, DateTime CreatedAt);
record ContactItem(Guid Id, string Name, string Email, string Phone, DateTime CreatedAt);

// SQLite Database Helper
static class Database
{
    // Global event for data changes - allows DashboardApp to react to updates
    public static event Action? DataChanged;

    private static void NotifyDataChanged() => DataChanged?.Invoke();

    private static async Task<SqliteConnection> GetConnectionAsync(IVolume volume)
    {
        var relativePath = "db.sqlite";
        var absolutePath = volume.GetAbsolutePath(relativePath);
        
        // Copy seed database from project folder if it doesn't exist
        if (!File.Exists(absolutePath))
        {
            var seedPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            if (File.Exists(seedPath))
            {
                var dir = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.Copy(seedPath, absolutePath);
            }
        }

        // Use relative path for connection (like autodealer-crm)
        // This works because the database is in the working directory
        var connection = new SqliteConnection($@"Data Source=""{relativePath}""");
        await connection.OpenAsync();
        return connection;
    }

    public static async Task<List<TaskItem>> GetTasksAsync(IVolume volume)
    {
        using var conn = await GetConnectionAsync(volume);
        using var cmd = new SqliteCommand("SELECT Id, Title, IsCompleted, CreatedAt FROM Tasks ORDER BY CreatedAt DESC", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        var tasks = new List<TaskItem>();
        while (await reader.ReadAsync())
            tasks.Add(new TaskItem(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetInt32(2) == 1, DateTime.Parse(reader.GetString(3))));
        return tasks;
    }

    public static async Task SaveTaskAsync(IVolume volume, TaskItem task) =>
        await ExecuteWithNotificationAsync(volume, "INSERT OR REPLACE INTO Tasks (Id, Title, IsCompleted, CreatedAt) VALUES (@Id, @Title, @IsCompleted, @CreatedAt)",
            ("@Id", task.Id.ToString()), ("@Title", task.Title), ("@IsCompleted", task.IsCompleted ? 1 : 0), ("@CreatedAt", task.CreatedAt.ToString("O")));

    public static async Task DeleteTaskAsync(IVolume volume, Guid id) =>
        await ExecuteWithNotificationAsync(volume, "DELETE FROM Tasks WHERE Id = @Id", ("@Id", id.ToString()));

    public static async Task<List<NoteItem>> GetNotesAsync(IVolume volume)
    {
        using var conn = await GetConnectionAsync(volume);
        using var cmd = new SqliteCommand("SELECT Id, Title, Content, CreatedAt FROM Notes ORDER BY CreatedAt DESC", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        var notes = new List<NoteItem>();
        while (await reader.ReadAsync())
            notes.Add(new NoteItem(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), DateTime.Parse(reader.GetString(3))));
        return notes;
    }

    public static async Task SaveNoteAsync(IVolume volume, NoteItem note) =>
        await ExecuteWithNotificationAsync(volume, "INSERT OR REPLACE INTO Notes (Id, Title, Content, CreatedAt) VALUES (@Id, @Title, @Content, @CreatedAt)",
            ("@Id", note.Id.ToString()), ("@Title", note.Title), ("@Content", note.Content), ("@CreatedAt", note.CreatedAt.ToString("O")));

    public static async Task DeleteNoteAsync(IVolume volume, Guid id) =>
        await ExecuteWithNotificationAsync(volume, "DELETE FROM Notes WHERE Id = @Id", ("@Id", id.ToString()));

    public static async Task<List<ContactItem>> GetContactsAsync(IVolume volume)
    {
        using var conn = await GetConnectionAsync(volume);
        using var cmd = new SqliteCommand("SELECT Id, Name, Email, Phone, CreatedAt FROM Contacts ORDER BY Name", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        var contacts = new List<ContactItem>();
        while (await reader.ReadAsync())
            contacts.Add(new ContactItem(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? "" : reader.GetString(3), DateTime.Parse(reader.GetString(4))));
        return contacts;
    }

    public static async Task SaveContactAsync(IVolume volume, ContactItem contact) =>
        await ExecuteWithNotificationAsync(volume, "INSERT OR REPLACE INTO Contacts (Id, Name, Email, Phone, CreatedAt) VALUES (@Id, @Name, @Email, @Phone, @CreatedAt)",
            ("@Id", contact.Id.ToString()), ("@Name", contact.Name), ("@Email", contact.Email), ("@Phone", string.IsNullOrEmpty(contact.Phone) ? DBNull.Value : contact.Phone), ("@CreatedAt", contact.CreatedAt.ToString("O")));

    public static async Task DeleteContactAsync(IVolume volume, Guid id) =>
        await ExecuteWithNotificationAsync(volume, "DELETE FROM Contacts WHERE Id = @Id", ("@Id", id.ToString()));

    private static async Task ExecuteAsync(IVolume volume, string sql, params (string, object)[] parameters)
    {
        using var conn = await GetConnectionAsync(volume);
        using var cmd = new SqliteCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteWithNotificationAsync(IVolume volume, string sql, params (string, object)[] parameters)
    {
        await ExecuteAsync(volume, sql, parameters);
        NotifyDataChanged();
    }
}

// Dashboard App
[App(icon: Icons.ChartBar, title: "Dashboard")]
public class DashboardApp : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var tasks = UseState<List<TaskItem>>(() => new List<TaskItem>());
        var notes = UseState<List<NoteItem>>(() => new List<NoteItem>());
        var contacts = UseState<List<ContactItem>>(() => new List<ContactItem>());

        var refreshToken = this.UseRefreshToken();

        // Load data on init, when refreshToken changes, and subscribe to global data changes
        UseEffect(async () =>
        {
            if (volume != null)
            {
                tasks.Value = await Database.GetTasksAsync(volume);
                notes.Value = await Database.GetNotesAsync(volume);
                contacts.Value = await Database.GetContactsAsync(volume);
            }
        }, [EffectTrigger.AfterInit(), refreshToken]);

        // Subscribe to global data changes from Database (for cross-app sync)
        UseEffect(() =>
        {
            void OnDataChanged() => refreshToken.Refresh();
            Database.DataChanged += OnDataChanged;
        }, [EffectTrigger.AfterInit()]);

        var completedTasks = tasks.Value.Count(t => t.IsCompleted);
        var totalTasks = tasks.Value.Count;
        var pendingTasks = totalTasks - completedTasks;

        // Prepare data for Task Status Pie Chart
        var taskStatusData = new[]
        {
            new { Status = "Completed", Count = completedTasks },
            new { Status = "Pending", Count = pendingTasks }
        }.Where(x => x.Count > 0).ToList();

        var taskStatusPieChart = taskStatusData.Count > 0
            ? taskStatusData.ToPieChart(
                dimension: x => x.Status,
                measure: x => x.Sum(f => f.Count),
                PieChartStyles.Dashboard,
                new PieChartTotal(totalTasks.ToString(), "Total Tasks"))
            : null;

        // Prepare data for Activity Over Time Bar Chart
        var activityData = tasks.Value.Select(t => new { Date = t.CreatedAt.Date })
            .Concat(notes.Value.Select(n => new { Date = n.CreatedAt.Date }))
            .Concat(contacts.Value.Select(c => new { Date = c.CreatedAt.Date }))
            .GroupBy(x => x.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToList();

        var activityBarChart = activityData.Count > 0
            ? activityData.ToBarChart()
                .Dimension("Date", x => x.Date.ToString("MMM dd"))
                .Measure("Items Created", x => x.Sum(f => f.Count))
            : null;

        return Layout.Vertical().Gap(4).Padding(4)
            | Text.H1("CRM Dashboard")
            | (Layout.Grid().Columns(3).Gap(3)
                | new Card(Text.H3(totalTasks.ToString())).Title("Total Tasks").Icon(Icons.ListTodo)
                | new Card(Text.H3(notes.Value.Count.ToString())).Title("Total Notes").Icon(Icons.FileText)
                | new Card(Text.H3(contacts.Value.Count.ToString())).Title("Total Contacts").Icon(Icons.Users))
            | (Layout.Grid().Columns(2).Gap(3)
                | (taskStatusPieChart != null
                    ? new Card(
                        Layout.Vertical().Gap(3).Padding(3)
                            | Text.H3("Task Status")
                            | Text.Muted("Distribution of completed vs pending tasks")
                            | taskStatusPieChart
                    )
                    : new Card(
                        Layout.Vertical().Gap(2).Padding(3)
                            | Text.Muted("No tasks data available")
                    ))
                | (activityBarChart != null
                    ? new Card(
                        Layout.Vertical().Gap(3).Padding(3)
                            | Text.H3("Activity Over Time")
                            | Text.Muted("Items created by date")
                            | activityBarChart
                    )
                    : new Card(
                        Layout.Vertical().Gap(2).Padding(3)
                            | Text.Muted("No activity data available")
                    )));
    }
}

// Tasks App with Blades
[App(icon: Icons.ListTodo, title: "Tasks")]
public class TasksApp : ViewBase
{
    public override object? Build() => this.UseBlades(() => new TaskListBlade(), "Tasks");
}

public class TaskListBlade : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var blades = this.UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var refreshKey = UseState(() => 0);

        this.UseEffect(() =>
        {
            if (refreshToken.ReturnValue is TaskItem newTask)
            {
                blades.Pop(this, true);
            }
            refreshKey.Value++;
        }, [refreshToken]);

        var onItemClick = new Action<Event<ListItem>>(e =>
        {
            var task = (TaskItem)e.Sender.Tag!;
            blades.Push(this, new TaskDetailsBlade(task.Id, refreshToken), task.Title);
        });

        ListItem CreateItem(TaskItem task) => new(
            title: task.Title,
            subtitle: task.IsCompleted ? "Completed" : "Pending",
            onClick: onItemClick,
            tag: task,
            icon: task.IsCompleted ? Icons.Check : Icons.Square
        );

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Add Task")
            .ToTrigger((isOpen) => new TaskCreateDialog(isOpen, volume, refreshToken));
        
        return new Fragment() { Key = $"task-list-{refreshKey.Value}" }
            | new FilteredListView<TaskItem>(
                fetchRecords: async filter =>
                {
                    var all = await Database.GetTasksAsync(volume);
                    if (string.IsNullOrWhiteSpace(filter)) return all.OrderByDescending(t => t.CreatedAt).ToArray();
                    return all.Where(t => t.Title.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(t => t.CreatedAt).ToArray();
                },
                createItem: CreateItem,
                toolButtons: createBtn,
                onFilterChanged: _ => blades.Pop(this)
            );
    }
}

public class TaskDetailsBlade(Guid taskId, RefreshToken token) : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var blades = UseContext<IBladeController>();
        var task = UseState<TaskItem?>(() => null);
        var (alertView, showAlert) = this.UseAlert();

        UseEffect(async () =>
        {
            var tasks = await Database.GetTasksAsync(volume);
            task.Value = tasks.FirstOrDefault(t => t.Id == taskId);
        }, [EffectTrigger.AfterInit(), token]);

        if (task.Value == null) return null;

        var taskValue = task.Value;

        var onDelete = () =>
        {
            showAlert("Are you sure you want to delete this task?", result =>
            {
                if (result.IsOk())
                {
                    Delete(volume, taskId);
                    token.Refresh();
                    blades.Pop(refresh: true);
                }
            }, "Delete Task", AlertButtonSet.OkCancel);
        };

        var onToggle = async () =>
        {
            try
            {
                await Database.SaveTaskAsync(volume, taskValue with { IsCompleted = !taskValue.IsCompleted });
                token.Refresh();
            }
            catch (Exception ex) { UseService<IClientProvider>().Toast(ex.Message, "Error"); }
        };

        var dropDown = Icons.Ellipsis
            .ToButton()
            .Ghost()
            .WithDropDown(
                MenuItem.Default(taskValue.IsCompleted ? "Mark as Pending" : "Mark as Completed")
                    .Icon(taskValue.IsCompleted ? Icons.Square : Icons.Check)
                    .HandleSelect(async _ => await onToggle()),
                MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(onDelete)
            );

        var editBtn = new Button("Edit")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Pencil)
            .Width(Size.Grow())
            .ToTrigger((isOpen) => new TaskEditSheet(isOpen, volume, token, taskValue.Id));

        var detailsCard = new Card(
            content: Layout.Vertical()
                | Text.H4("Task Details")
                | new
                {
                    Id = taskValue.Id.ToString(),
                    Title = taskValue.Title,
                    Status = taskValue.IsCompleted ? "Completed" : "Pending",
                    CreatedAt = taskValue.CreatedAt.ToString("g")
                }
                .ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Width(Size.Full()).Gap(1).Align(Align.Right)
                | dropDown
                | editBtn
        ).Width(Size.Units(100));

        return new Fragment()
            | (Layout.Vertical() | detailsCard!)
            | alertView!;
    }

    private void Delete(IVolume volume, Guid taskId)
    {
        Database.DeleteTaskAsync(volume, taskId).Wait();
    }
}

public class TaskEditSheet(IState<bool> isOpen, IVolume volume, RefreshToken refreshToken, Guid taskId) : ViewBase
{
    public override object? Build()
    {
        var task = UseState<TaskItem?>(() => null);
        var client = UseService<IClientProvider>();
        var skipSave = UseState(() => true);

        UseEffect(async () =>
        {
            var tasks = await Database.GetTasksAsync(volume);
            task.Value = tasks.FirstOrDefault(t => t.Id == taskId);
            skipSave.Value = false;
        }, [EffectTrigger.AfterInit()]);

        UseEffect(async () =>
        {
            if (skipSave.Value || task.Value == null || string.IsNullOrWhiteSpace(task.Value.Title)) return;
            try
            {
                await Database.SaveTaskAsync(volume, task.Value);
                refreshToken.Refresh();
            }
            catch (Exception ex) { client.Toast(ex.Message, "Error"); }
        }, [task]);

        if (task.Value == null) return null;

        return task
            .ToForm()
            .Builder(t => t!.Title, e => e.ToTextInput())
            .Builder(t => t!.IsCompleted, e => e.ToSwitchInput())
            .Remove(t => t!.Id, t => t!.CreatedAt)
            .ToSheet(isOpen, "Edit Task");
    }
}

public class TaskCreateDialog(IState<bool> isOpen, IVolume volume, RefreshToken refreshToken) : ViewBase
{
    private record TaskCreateRequest
    {
        [Required]
        public string Title { get; init; } = "";
    }
    
    public override object? Build()
    {
        var task = UseState(() => new TaskCreateRequest());
        var client = UseService<IClientProvider>();

        UseEffect(async () =>
        {
            if (!string.IsNullOrWhiteSpace(task.Value.Title))
            {
                try
                {
                    var newTask = new TaskItem(Guid.NewGuid(), task.Value.Title.Trim(), false, DateTime.UtcNow);
                    await Database.SaveTaskAsync(volume, newTask);
                    refreshToken.Refresh(returnValue: newTask);
                    task.Set(new TaskCreateRequest());
                }
                catch (Exception ex) { client.Toast(ex.Message, "Error"); }
            }
        }, [task]);

        return task
            .ToForm()
            .Required(t => t.Title)
            .ToDialog(isOpen, title: "New Task", submitTitle: "Create");
    }
}

// Notes App with Blades
[App(icon: Icons.FileText, title: "Notes")]
public class NotesApp : ViewBase
{
    public override object? Build() => this.UseBlades(() => new NoteListBlade(), "Notes");
}

public class NoteListBlade : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var blades = UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var refreshKey = UseState(() => 0);

        this.UseEffect(() =>
        {
            if (refreshToken.ReturnValue is NoteItem newNote)
            {
                blades.Pop(this, true);
            }
            refreshKey.Value++;
        }, [refreshToken]);

        var onItemClick = new Action<Event<ListItem>>(e =>
        {
            var note = (NoteItem)e.Sender.Tag!;
            blades.Push(this, new NoteDetailsBlade(note.Id, refreshToken), note.Title);
        });

        ListItem CreateItem(NoteItem note) => new(
            title: note.Title,
            subtitle: string.IsNullOrWhiteSpace(note.Content) ? "(empty)" : note.Content.Length > 50 ? note.Content[..50] + "..." : note.Content,
            onClick: onItemClick,
            tag: note,
            icon: Icons.FileText
        );

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Add Note")
            .ToTrigger((isOpen) => new NoteCreateDialog(isOpen, volume, refreshToken));
        
        return new Fragment() { Key = $"note-list-{refreshKey.Value}" }
            | new FilteredListView<NoteItem>(
                fetchRecords: async filter =>
                {
                    var all = await Database.GetNotesAsync(volume);
                    if (string.IsNullOrWhiteSpace(filter)) return all.OrderByDescending(n => n.CreatedAt).ToArray();
                    filter = filter.Trim();
                    return all.Where(n => n.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) || 
                                         n.Content.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(n => n.CreatedAt).ToArray();
                },
                createItem: CreateItem,
                toolButtons: createBtn,
                onFilterChanged: _ => blades.Pop(this)
            );
    }
}

public class NoteDetailsBlade(Guid noteId, RefreshToken token) : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var blades = UseContext<IBladeController>();
        var note = UseState<NoteItem?>(() => null);
        var (alertView, showAlert) = this.UseAlert();

        UseEffect(async () =>
        {
            var notes = await Database.GetNotesAsync(volume);
            note.Value = notes.FirstOrDefault(n => n.Id == noteId);
        }, [EffectTrigger.AfterInit(), token]);

        if (note.Value == null) return null;

        var noteValue = note.Value;

        var onDelete = () =>
        {
            showAlert("Are you sure you want to delete this note?", result =>
            {
                if (result.IsOk())
                {
                    Delete(volume);
                    token.Refresh();
                    blades.Pop(refresh: true);
                }
            }, "Delete Note", AlertButtonSet.OkCancel);
        };

        var editBtn = new Button("Edit")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Pencil)
            .Width(Size.Grow())
            .ToTrigger((isOpen) => new NoteEditSheet(isOpen, volume, token, noteId));

        var deleteBtn = new Button("Delete")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Trash)
            .Width(Size.Grow())
            .HandleClick(onDelete);

        var detailsCard = new Card(
            content: Layout.Vertical()
                | Text.H4("Note Details")
                | new
                {
                    Id = noteValue.Id.ToString(),
                    Title = noteValue.Title,
                    Content = string.IsNullOrWhiteSpace(noteValue.Content) ? "(empty)" : noteValue.Content,
                    CreatedAt = noteValue.CreatedAt.ToString("g")
                }
                .ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard())
                .MultiLine(e => e.Content),
            footer: Layout.Horizontal().Width(Size.Full()).Gap(1).Align(Align.Right)
                | editBtn
                | deleteBtn
        ).Width(Size.Units(100));

        return new Fragment()
            | (Layout.Vertical() | detailsCard!)
            | alertView!;
    }

    private void Delete(IVolume volume)
    {
        Database.DeleteNoteAsync(volume, noteId).Wait();
    }
}

public class NoteCreateDialog(IState<bool> isOpen, IVolume volume, RefreshToken refreshToken) : ViewBase
{
    private record NoteCreateRequest
    {
        [Required]
        public string Title { get; init; } = "";
        public string Content { get; init; } = "";
    }
    
    public override object? Build()
    {
        var note = UseState(() => new NoteCreateRequest());
        var client = UseService<IClientProvider>();

        UseEffect(async () =>
        {
            if (!string.IsNullOrWhiteSpace(note.Value.Title))
            {
                try
                {
                    var newNote = new NoteItem(Guid.NewGuid(), note.Value.Title.Trim(), note.Value.Content ?? "", DateTime.UtcNow);
                    await Database.SaveNoteAsync(volume, newNote);
                    refreshToken.Refresh(returnValue: newNote);
                    note.Set(new NoteCreateRequest());
                }
                catch (Exception ex) { client.Toast(ex.Message, "Error"); }
            }
        }, [note]);

        return note
            .ToForm()
            .Required(n => n.Title)
            .Builder(n => n.Content, e => e.ToTextAreaInput().Height(Size.Units(8)))
            .ToDialog(isOpen, title: "New Note", submitTitle: "Create");
    }
}

public class NoteEditSheet(IState<bool> isOpen, IVolume volume, RefreshToken refreshToken, Guid noteId) : ViewBase
{
    public override object? Build()
    {
        var note = UseState<NoteItem?>(() => null);
        var client = UseService<IClientProvider>();
        var skipSave = UseState(() => true);

        UseEffect(async () =>
        {
            var notes = await Database.GetNotesAsync(volume);
            note.Value = notes.FirstOrDefault(n => n.Id == noteId);
            skipSave.Value = false;
        }, [EffectTrigger.AfterInit()]);

        UseEffect(async () =>
        {
            if (skipSave.Value || note.Value == null || string.IsNullOrWhiteSpace(note.Value.Title)) return;
            try
            {
                await Database.SaveNoteAsync(volume, note.Value);
                refreshToken.Refresh();
            }
            catch (Exception ex) { client.Toast(ex.Message, "Error"); }
        }, [note]);

        if (note.Value == null) return null;

        return note
            .ToForm()
            .Builder(n => n!.Title, e => e.ToTextInput())
            .Builder(n => n!.Content, e => e.ToTextAreaInput().Height(Size.Units(8)))
            .Remove(n => n!.Id, n => n!.CreatedAt)
            .ToSheet(isOpen, "Edit Note");
    }
}

// Contacts App with Blades
[App(icon: Icons.Users, title: "Contacts")]
public class ContactsApp : ViewBase
{
    public override object? Build() => this.UseBlades(() => new ContactListBlade(), "Contacts");
}

public class ContactListBlade : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var blades = UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var refreshKey = UseState(() => 0);

        this.UseEffect(() =>
        {
            if (refreshToken.ReturnValue is ContactItem newContact)
            {
                blades.Pop(this, true);
            }
            refreshKey.Value++;
        }, [refreshToken]);

        var onItemClick = new Action<Event<ListItem>>(e =>
        {
            var contact = (ContactItem)e.Sender.Tag!;
            blades.Push(this, new ContactDetailsBlade(contact.Id, refreshToken), contact.Name);
        });

        ListItem CreateItem(ContactItem contact) => new(
            title: contact.Name,
            subtitle: contact.Email,
            onClick: onItemClick,
            tag: contact,
            icon: Icons.Users
        );

        var createBtn = Icons.Plus.ToButton(_ =>
        {
            blades.Pop(this);
        }).Ghost().Tooltip("Add Contact")
            .ToTrigger((isOpen) => new ContactCreateDialog(isOpen, volume, refreshToken));
        
        return new Fragment() { Key = $"contact-list-{refreshKey.Value}" }
            | new FilteredListView<ContactItem>(
                fetchRecords: async filter =>
                {
                    var all = await Database.GetContactsAsync(volume);
                    if (string.IsNullOrWhiteSpace(filter)) return all.OrderBy(c => c.Name).ToArray();
                    filter = filter.Trim();
                    return all.Where(c => c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) || 
                                         c.Email.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                                         (c.Phone != null && c.Phone.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(c => c.Name).ToArray();
                },
                createItem: CreateItem,
                toolButtons: createBtn,
                onFilterChanged: _ => blades.Pop(this)
            );
    }
}

public class ContactDetailsBlade(Guid contactId, RefreshToken token) : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var blades = UseContext<IBladeController>();
        var contact = UseState<ContactItem?>(() => null);
        var (alertView, showAlert) = this.UseAlert();

        UseEffect(async () =>
        {
            var contacts = await Database.GetContactsAsync(volume);
            contact.Value = contacts.FirstOrDefault(c => c.Id == contactId);
        }, [EffectTrigger.AfterInit(), token]);

        if (contact.Value == null) return null;

        var contactValue = contact.Value;

        var onDelete = () =>
        {
            showAlert("Are you sure you want to delete this contact?", result =>
            {
                if (result.IsOk())
                {
                    Delete(volume);
                    token.Refresh();
                    blades.Pop(refresh: true);
                }
            }, "Delete Contact", AlertButtonSet.OkCancel);
        };

        var editBtn = new Button("Edit")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Pencil)
            .Width(Size.Grow())
            .ToTrigger((isOpen) => new ContactEditSheet(isOpen, volume, token, contactId));

        var deleteBtn = new Button("Delete")
            .Variant(ButtonVariant.Outline)
            .Icon(Icons.Trash)
            .Width(Size.Grow())
            .HandleClick(onDelete);

        var detailsCard = new Card(
            content: Layout.Vertical()
                | Text.H4("Contact Details")
                | new
                {
                    Id = contactValue.Id.ToString(),
                    Name = contactValue.Name,
                    Email = contactValue.Email,
                    Phone = string.IsNullOrEmpty(contactValue.Phone) ? "N/A" : contactValue.Phone,
                    CreatedAt = contactValue.CreatedAt.ToString("g")
                }
                .ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Width(Size.Full()).Gap(1).Align(Align.Right)
                | editBtn
                | deleteBtn
        ).Width(Size.Units(100));

        return new Fragment()
            | (Layout.Vertical() | detailsCard!)
            | alertView!;
    }

    private void Delete(IVolume volume)
    {
        Database.DeleteContactAsync(volume, contactId).Wait();
    }
}

public class ContactCreateDialog(IState<bool> isOpen, IVolume volume, RefreshToken refreshToken) : ViewBase
{
    private record ContactCreateRequest
    {
        [Required]
        public string Name { get; init; } = "";
        [Required]
        [EmailAddress]
        public string Email { get; init; } = "";
        public string? Phone { get; init; }
    }
    
    public override object? Build()
    {
        var contact = UseState(() => new ContactCreateRequest());
        var client = UseService<IClientProvider>();

        UseEffect(async () =>
        {
            if (!string.IsNullOrWhiteSpace(contact.Value.Name) && !string.IsNullOrWhiteSpace(contact.Value.Email))
            {
                try
                {
                    var newContact = new ContactItem(Guid.NewGuid(), contact.Value.Name.Trim(), contact.Value.Email.Trim(), contact.Value.Phone?.Trim() ?? "", DateTime.UtcNow);
                    await Database.SaveContactAsync(volume, newContact);
                    refreshToken.Refresh(returnValue: newContact);
                    contact.Set(new ContactCreateRequest());
                }
                catch (Exception ex) { client.Toast(ex.Message, "Error"); }
            }
        }, [contact]);

        return contact
            .ToForm()
            .Required(c => c.Name, c => c.Email)
            .Builder(c => c.Email, e => e.ToEmailInput())
            .ToDialog(isOpen, title: "New Contact", submitTitle: "Create");
    }
}

public class ContactEditSheet(IState<bool> isOpen, IVolume volume, RefreshToken refreshToken, Guid contactId) : ViewBase
{
    public override object? Build()
    {
        var contact = UseState<ContactItem?>(() => null);
        var client = UseService<IClientProvider>();
        var skipSave = UseState(() => true);

        UseEffect(async () =>
        {
            var contacts = await Database.GetContactsAsync(volume);
            contact.Value = contacts.FirstOrDefault(c => c.Id == contactId);
            skipSave.Value = false;
        }, [EffectTrigger.AfterInit()]);

        UseEffect(async () =>
        {
            if (skipSave.Value || contact.Value == null || string.IsNullOrWhiteSpace(contact.Value.Name) || string.IsNullOrWhiteSpace(contact.Value.Email)) return;
            try
            {
                await Database.SaveContactAsync(volume, contact.Value);
                refreshToken.Refresh();
            }
            catch (Exception ex) { client.Toast(ex.Message, "Error"); }
        }, [contact]);

        if (contact.Value == null) return null;

        return contact
            .ToForm()
            .Builder(c => c!.Name, e => e.ToTextInput())
            .Builder(c => c!.Email, e => e.ToEmailInput())
            .Builder(c => c!.Phone, e => e.ToTextInput())
            .Remove(c => c!.Id, c => c!.CreatedAt)
            .ToSheet(isOpen, "Edit Contact");
    }
}

