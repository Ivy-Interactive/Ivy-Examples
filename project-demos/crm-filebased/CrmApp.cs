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
global using Ivy.Views.Blades;
global using Ivy.Views.Forms;
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

var chromeSettings = new ChromeSettings()
    .DefaultApp<DashboardApp>()
    .UseTabs(preventDuplicates: true);

server.UseChrome(chromeSettings);
server.UseVolume(new FolderVolume(Ivy.Utils.IsProduction() ? "/app/data" : null));
await server.RunAsync();

// Models
record TaskItem(Guid Id, string Title, bool IsCompleted, DateTime CreatedAt);
record NoteItem(Guid Id, string Title, string Content, DateTime CreatedAt);
record ContactItem(Guid Id, string Name, string Email, string? Phone, DateTime CreatedAt);

// SQLite Database Helper
static class Database
{
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
        await ExecuteAsync(volume, "INSERT OR REPLACE INTO Tasks (Id, Title, IsCompleted, CreatedAt) VALUES (@Id, @Title, @IsCompleted, @CreatedAt)",
            ("@Id", task.Id.ToString()), ("@Title", task.Title), ("@IsCompleted", task.IsCompleted ? 1 : 0), ("@CreatedAt", task.CreatedAt.ToString("O")));

    public static async Task DeleteTaskAsync(IVolume volume, Guid id) =>
        await ExecuteAsync(volume, "DELETE FROM Tasks WHERE Id = @Id", ("@Id", id.ToString()));

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
        await ExecuteAsync(volume, "INSERT OR REPLACE INTO Notes (Id, Title, Content, CreatedAt) VALUES (@Id, @Title, @Content, @CreatedAt)",
            ("@Id", note.Id.ToString()), ("@Title", note.Title), ("@Content", note.Content), ("@CreatedAt", note.CreatedAt.ToString("O")));

    public static async Task DeleteNoteAsync(IVolume volume, Guid id) =>
        await ExecuteAsync(volume, "DELETE FROM Notes WHERE Id = @Id", ("@Id", id.ToString()));

    public static async Task<List<ContactItem>> GetContactsAsync(IVolume volume)
    {
        using var conn = await GetConnectionAsync(volume);
        using var cmd = new SqliteCommand("SELECT Id, Name, Email, Phone, CreatedAt FROM Contacts ORDER BY Name", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        var contacts = new List<ContactItem>();
        while (await reader.ReadAsync())
            contacts.Add(new ContactItem(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), DateTime.Parse(reader.GetString(4))));
        return contacts;
    }

    public static async Task SaveContactAsync(IVolume volume, ContactItem contact) =>
        await ExecuteAsync(volume, "INSERT OR REPLACE INTO Contacts (Id, Name, Email, Phone, CreatedAt) VALUES (@Id, @Name, @Email, @Phone, @CreatedAt)",
            ("@Id", contact.Id.ToString()), ("@Name", contact.Name), ("@Email", contact.Email), ("@Phone", (object?)contact.Phone ?? DBNull.Value), ("@CreatedAt", contact.CreatedAt.ToString("O")));

    public static async Task DeleteContactAsync(IVolume volume, Guid id) =>
        await ExecuteAsync(volume, "DELETE FROM Contacts WHERE Id = @Id", ("@Id", id.ToString()));

    private static async Task ExecuteAsync(IVolume volume, string sql, params (string, object)[] parameters)
    {
        using var conn = await GetConnectionAsync(volume);
        using var cmd = new SqliteCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        await cmd.ExecuteNonQueryAsync();
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

        UseEffect(async () =>
        {
            if (volume != null)
            {
                tasks.Value = await Database.GetTasksAsync(volume);
                notes.Value = await Database.GetNotesAsync(volume);
                contacts.Value = await Database.GetContactsAsync(volume);
            }
        }, [EffectTrigger.AfterInit()]);

        var completedTasks = tasks.Value.Count(t => t.IsCompleted);
        var totalTasks = tasks.Value.Count;
        var pendingTasks = totalTasks - completedTasks;

        return Layout.Vertical().Gap(4).Padding(4)
            | Text.H1("CRM Dashboard")
            | Layout.Grid().Columns(3).Gap(3)
                | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                    | Text.H2(totalTasks.ToString()).Bold()
                    | Text.Muted("Total Tasks")
                    | Text.Small($"{completedTasks} completed, {pendingTasks} pending")
                ).Icon(Icons.ListTodo)
                | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                    | Text.H2(notes.Value.Count.ToString()).Bold()
                    | Text.Muted("Total Notes")
                ).Icon(Icons.FileText)
                | new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                    | Text.H2(contacts.Value.Count.ToString()).Bold()
                    | Text.Muted("Total Contacts")
                ).Icon(Icons.Users);
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
        var blades = UseContext<IBladeController>();
        var tasks = UseState<List<TaskItem>>(() => new List<TaskItem>());
        var client = UseService<IClientProvider>();

        UseEffect(async () => tasks.Value = await Database.GetTasksAsync(volume), [EffectTrigger.AfterInit()]);

        var onItemClick = new Action<Event<ListItem>>(e =>
        {
            var task = (TaskItem)e.Sender.Tag!;
            blades.Push(this, new TaskDetailsBlade(task.Id), task.Title);
        });

        ListItem CreateItem(TaskItem task) => new(
            title: task.Title,
            subtitle: task.IsCompleted ? "Completed" : "Pending",
            onClick: onItemClick,
            tag: task,
            icon: task.IsCompleted ? Icons.Check : Icons.Square
        );

        var refreshToken = this.UseRefreshToken();
        var createBtn = Icons.Plus.ToButton(_ => { }).Ghost().Tooltip("Add Task")
            .ToTrigger(isOpen => new TaskCreateDialog(isOpen, volume, refreshToken));

        return new FilteredListView<TaskItem>(
            fetchRecords: async filter =>
            {
                var all = await Database.GetTasksAsync(volume);
                if (string.IsNullOrWhiteSpace(filter)) return all.OrderByDescending(t => t.CreatedAt).ToArray();
                filter = filter.Trim();
                return all.Where(t => t.Title.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(t => t.CreatedAt).ToArray();
            },
            createItem: CreateItem,
            toolButtons: createBtn,
            onFilterChanged: _ => blades.Pop(this)
        );
    }
}

public class TaskDetailsBlade(Guid taskId) : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var blades = UseContext<IBladeController>();
        var task = UseState<TaskItem?>(() => null);
        var editTitle = UseState<string>();
        var isEditing = UseState<bool>(() => false);
        var client = UseService<IClientProvider>();

        UseEffect(async () =>
        {
            var tasks = await Database.GetTasksAsync(volume);
            task.Value = tasks.FirstOrDefault(t => t.Id == taskId);
        }, [EffectTrigger.AfterInit()]);

        if (task.Value == null) return Text.Muted("Loading...");

        var t = task.Value;
        
        if (isEditing.Value && editTitle.Value == "")
        {
            editTitle.Value = t.Title;
        }

        async Task Save()
        {
            if (string.IsNullOrWhiteSpace(editTitle.Value)) return;
            try
            {
                await Database.SaveTaskAsync(volume, t with { Title = editTitle.Value.Trim() });
                blades.Pop(refresh: true);
            }
            catch (Exception ex) { client.Toast(ex.Message, "Error"); }
        }

        async Task Toggle()
        {
            try
            {
                await Database.SaveTaskAsync(volume, t with { IsCompleted = !t.IsCompleted });
                blades.Pop(refresh: true);
            }
            catch (Exception ex) { client.Toast(ex.Message, "Error"); }
        }

        async Task Delete()
        {
            try
            {
                await Database.DeleteTaskAsync(volume, taskId);
                blades.Pop(refresh: true);
            }
            catch (Exception ex) { client.Toast(ex.Message, "Error"); }
        }

        return new Card(
            Layout.Vertical().Gap(3).Padding(3)
            | Layout.Horizontal().Gap(2).Width(Size.Full())
                | Text.H3(t.Title).Bold()
                | new Spacer()
                | new Button("", _ => isEditing.Value = true).Icon(Icons.Pencil).Ghost()
                | new Button("", async _ => await Delete()).Icon(Icons.Trash).Ghost()
            | (isEditing.Value
                ? Layout.Vertical().Gap(2)
                    | editTitle.ToInput(placeholder: "Task title...")
                    | Layout.Horizontal().Gap(2)
                        | new Button("Save", async _ => await Save()).Primary()
                        | new Button("Cancel", _ => isEditing.Value = false).Ghost()
                : Layout.Vertical().Gap(2)
                    | Layout.Horizontal().Gap(2)
                        | new Button(t.IsCompleted ? "Mark as Pending" : "Mark as Completed", async _ => await Toggle())
                            .Icon(t.IsCompleted ? Icons.Check : Icons.Square)
                        | Text.Small($"Created: {t.CreatedAt:g}")
            )
        ).Title("Task Details").Width(Size.Units(100));
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
                    await Database.SaveTaskAsync(volume, new TaskItem(Guid.NewGuid(), task.Value.Title.Trim(), false, DateTime.UtcNow));
                    refreshToken.Refresh();
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
        var notes = UseState<List<NoteItem>>(() => new List<NoteItem>());

        UseEffect(async () => notes.Value = await Database.GetNotesAsync(volume), [EffectTrigger.AfterInit()]);

        var onItemClick = new Action<Event<ListItem>>(e =>
        {
            var note = (NoteItem)e.Sender.Tag!;
            blades.Push(this, new NoteDetailsBlade(note.Id), note.Title);
        });

        ListItem CreateItem(NoteItem note) => new(
            title: note.Title,
            subtitle: string.IsNullOrWhiteSpace(note.Content) ? "(empty)" : note.Content.Length > 50 ? note.Content[..50] + "..." : note.Content,
            onClick: onItemClick,
            tag: note,
            icon: Icons.FileText
        );

        var refreshToken = this.UseRefreshToken();
        var createBtn = Icons.Plus.ToButton(_ => { }).Ghost().Tooltip("Add Note")
            .ToTrigger(isOpen => new NoteCreateDialog(isOpen, volume, refreshToken));

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue != null)
                notes.Value = Database.GetNotesAsync(volume).Result;
        }, [refreshToken]);

        return new FilteredListView<NoteItem>(
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

public class NoteDetailsBlade(Guid noteId) : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var blades = UseContext<IBladeController>();
        var note = UseState<NoteItem?>(() => null);
        var editTitle = UseState<string>();
        var editContent = UseState<string>();
        var isEditing = UseState<bool>(() => false);
        var client = UseService<IClientProvider>();

        UseEffect(async () =>
        {
            var notes = await Database.GetNotesAsync(volume);
            note.Value = notes.FirstOrDefault(n => n.Id == noteId);
        }, [EffectTrigger.AfterInit()]);

        if (note.Value == null) return Text.Muted("Loading...");

        var n = note.Value;

        async Task Save()
        {
            if (string.IsNullOrWhiteSpace(editTitle.Value)) return;
            try
            {
                await Database.SaveNoteAsync(volume, n with { Title = editTitle.Value.Trim(), Content = editContent.Value ?? "" });
                blades.Pop(refresh: true);
            }
            catch (Exception ex) { client.Toast(ex.Message, "Error"); }
        }

        async Task Delete()
        {
            try
            {
                await Database.DeleteNoteAsync(volume, noteId);
                blades.Pop(refresh: true);
            }
            catch (Exception ex) { client.Toast(ex.Message, "Error"); }
        }

        if (isEditing.Value)
        {
            editTitle.Value = n.Title;
            editContent.Value = n.Content;
        }

        return new Card(
            Layout.Vertical().Gap(3).Padding(3)
            | Layout.Horizontal().Gap(2).Width(Size.Full())
                | Text.H3(n.Title).Bold()
                | new Spacer()
                | new Button("", _ => { isEditing.Value = true; editTitle.Value = n.Title; editContent.Value = n.Content; }).Icon(Icons.Pencil).Ghost()
                | new Button("", async _ => await Delete()).Icon(Icons.Trash).Ghost()
            | (isEditing.Value
                ? Layout.Vertical().Gap(2)
                    | editTitle.ToInput(placeholder: "Note title...")
                    | editContent.ToTextAreaInput(placeholder: "Note content...").Height(Size.Units(10))
                    | Layout.Horizontal().Gap(2)
                        | new Button("Save", async _ => await Save()).Primary()
                        | new Button("Cancel", _ => isEditing.Value = false).Ghost()
                : Layout.Vertical().Gap(2)
                    | (string.IsNullOrWhiteSpace(n.Content) ? Text.Muted("(empty)") : Text.Block(n.Content))
                    | Text.Small($"Created: {n.CreatedAt:g}")
            )
        ).Title("Note Details").Width(Size.Units(100));
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
                    await Database.SaveNoteAsync(volume, new NoteItem(Guid.NewGuid(), note.Value.Title.Trim(), note.Value.Content ?? "", DateTime.UtcNow));
                    refreshToken.Refresh();
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
        var contacts = UseState<List<ContactItem>>(() => new List<ContactItem>());

        UseEffect(async () => contacts.Value = await Database.GetContactsAsync(volume), [EffectTrigger.AfterInit()]);

        var onItemClick = new Action<Event<ListItem>>(e =>
        {
            var contact = (ContactItem)e.Sender.Tag!;
            blades.Push(this, new ContactDetailsBlade(contact.Id), contact.Name);
        });

        ListItem CreateItem(ContactItem contact) => new(
            title: contact.Name,
            subtitle: contact.Email,
            onClick: onItemClick,
            tag: contact,
            icon: Icons.Users
        );

        var refreshToken = this.UseRefreshToken();
        var createBtn = Icons.Plus.ToButton(_ => { }).Ghost().Tooltip("Add Contact")
            .ToTrigger(isOpen => new ContactCreateDialog(isOpen, volume, refreshToken));

        UseEffect(() =>
        {
            if (refreshToken.ReturnValue != null)
                contacts.Value = Database.GetContactsAsync(volume).Result;
        }, [refreshToken]);

        return new FilteredListView<ContactItem>(
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

public class ContactDetailsBlade(Guid contactId) : ViewBase
{
    public override object? Build()
    {
        var volume = UseService<IVolume>();
        var blades = UseContext<IBladeController>();
        var contact = UseState<ContactItem?>(() => null);
        var editName = UseState<string>();
        var editEmail = UseState<string>();
        var editPhone = UseState<string>();
        var isEditing = UseState<bool>(() => false);
        var client = UseService<IClientProvider>();

        UseEffect(async () =>
        {
            var contacts = await Database.GetContactsAsync(volume);
            contact.Value = contacts.FirstOrDefault(c => c.Id == contactId);
        }, [EffectTrigger.AfterInit()]);

        if (contact.Value == null) return Text.Muted("Loading...");

        var c = contact.Value;

        async Task Save()
        {
            if (string.IsNullOrWhiteSpace(editName.Value) || string.IsNullOrWhiteSpace(editEmail.Value)) return;
            try
            {
                await Database.SaveContactAsync(volume, c with { Name = editName.Value.Trim(), Email = editEmail.Value.Trim(), Phone = editPhone.Value?.Trim() });
                blades.Pop(refresh: true);
            }
            catch (Exception ex) { client.Toast(ex.Message, "Error"); }
        }

        async Task Delete()
        {
            try
            {
                await Database.DeleteContactAsync(volume, contactId);
                blades.Pop(refresh: true);
            }
            catch (Exception ex) { client.Toast(ex.Message, "Error"); }
        }

        if (isEditing.Value)
        {
            editName.Value = c.Name;
            editEmail.Value = c.Email;
            editPhone.Value = c.Phone ?? "";
        }

        return new Card(
            Layout.Vertical().Gap(3).Padding(3)
            | Layout.Horizontal().Gap(2).Width(Size.Full())
                | Text.H3(c.Name).Bold()
                | new Spacer()
                | new Button("", _ => { isEditing.Value = true; editName.Value = c.Name; editEmail.Value = c.Email; editPhone.Value = c.Phone ?? ""; }).Icon(Icons.Pencil).Ghost()
                | new Button("", async _ => await Delete()).Icon(Icons.Trash).Ghost()
            | (isEditing.Value
                ? Layout.Vertical().Gap(2)
                    | editName.ToInput(placeholder: "Name...")
                    | editEmail.ToInput(placeholder: "Email...")
                    | editPhone.ToInput(placeholder: "Phone (optional)...")
                    | Layout.Horizontal().Gap(2)
                        | new Button("Save", async _ => await Save()).Primary()
                        | new Button("Cancel", _ => isEditing.Value = false).Ghost()
                : Layout.Vertical().Gap(2)
                    | Text.Block(c.Email)
                    | (string.IsNullOrWhiteSpace(c.Phone) ? new Spacer() : Text.Block(c.Phone))
                    | Text.Small($"Created: {c.CreatedAt:g}")
            )
        ).Title("Contact Details").Width(Size.Units(100));
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
                    await Database.SaveContactAsync(volume, new ContactItem(Guid.NewGuid(), contact.Value.Name.Trim(), contact.Value.Email.Trim(), contact.Value.Phone?.Trim(), DateTime.UtcNow));
                    refreshToken.Refresh();
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
