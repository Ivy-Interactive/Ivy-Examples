namespace AutodealerCrm.Apps.Views;

public class TaskEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int taskId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var task = UseState(() => factory.CreateDbContext().Tasks.FirstOrDefault(e => e.Id == taskId)!);

        UseEffect(() =>
        {
            using var db = factory.CreateDbContext();
            task.Value.UpdatedAt = DateTime.UtcNow;
            db.Tasks.Update(task.Value);
            db.SaveChanges();
            refreshToken.Refresh();
        }, [task]);

        return task
            .ToForm()
            .Builder(e => e.Title, e => e.ToTextInput())
            .Builder(e => e.Description, e => e.ToTextAreaInput())
            .Builder(e => e.DueDate, e => e.ToDateInput())
            .Builder(e => e.Completed, e => e.ToSwitchInput())
            .Builder(e => e.LeadId, e => e.ToAsyncSelectInput(QueryLeads(factory), LookupLead(factory), placeholder: "Select Lead"))
            .Builder(e => e.ManagerId, e => e.ToAsyncSelectInput(QueryManagers(factory), LookupManager(factory), placeholder: "Select Manager"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Task");
    }

    private static AsyncSelectQueryDelegate<int?> QueryLeads(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Leads
                    .Where(e => e.Customer.FirstName.Contains(query) || e.Customer.LastName.Contains(query))
                    .Select(e => new { e.Id, Name = e.Customer.FirstName + " " + e.Customer.LastName })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.Name, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupLead(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var lead = await db.Leads.Include(e => e.Customer).FirstOrDefaultAsync(e => e.Id == id);
            if (lead == null) return null;
            return new Option<int?>(lead.Customer.FirstName + " " + lead.Customer.LastName, lead.Id);
        };
    }

    private static AsyncSelectQueryDelegate<int?> QueryManagers(AutodealerCrmContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Users
                    .Where(e => e.Name.Contains(query))
                    .Select(e => new { e.Id, e.Name })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.Name, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupManager(AutodealerCrmContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var manager = await db.Users.FirstOrDefaultAsync(e => e.Id == id);
            if (manager == null) return null;
            return new Option<int?>(manager.Name, manager.Id);
        };
    }
}