namespace AutodealerCrm.Apps.Views;

public class LeadDetailsBlade(int leadId) : ViewBase
{
    public override object? Build()
    {
        var factory = this.UseService<AutodealerCrmContextFactory>();
        var blades = this.UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken();
        var lead = this.UseState<Lead?>();
        var callRecordCount = this.UseState<int>();
        var mediaCount = this.UseState<int>();
        var messageCount = this.UseState<int>();
        var taskCount = this.UseState<int>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            lead.Set(await db.Leads
                .Include(e => e.Customer)
                .Include(e => e.Manager)
                .Include(e => e.SourceChannel)
                .Include(e => e.LeadIntent)
                .Include(e => e.LeadStage)
                .SingleOrDefaultAsync(e => e.Id == leadId));
            callRecordCount.Set(await db.CallRecords.CountAsync(e => e.LeadId == leadId));
            mediaCount.Set(await db.Media.CountAsync(e => e.LeadId == leadId));
            messageCount.Set(await db.Messages.CountAsync(e => e.LeadId == leadId));
            taskCount.Set(await db.Tasks.CountAsync(e => e.LeadId == leadId));
        }, [EffectTrigger.AfterInit(), refreshToken]);

        if (lead.Value == null) return null;

        var leadValue = lead.Value;

        void OnDelete()
        {
            showAlert("Are you sure you want to delete this lead?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete Lead", AlertButtonSet.OkCancel);
        };

        var dropDown = Icons.Ellipsis
            .ToButton()
            .Ghost()
            .WithDropDown(
                MenuItem.Default("Delete").Icon(Icons.Trash).HandleSelect(OnDelete)
            );

        var editBtn = new Button("Edit")
            .Outline()
            .Icon(Icons.Pencil)
            .ToTrigger((isOpen) => new LeadEditSheet(isOpen, refreshToken, leadId));

        var detailsCard = new Card(
            content: new
                {
                    leadValue.Id,
                    CustomerName = $"{leadValue.Customer.FirstName} {leadValue.Customer.LastName}",
                    ManagerName = leadValue.Manager?.Name ?? "Unassigned",
                    SourceChannel = leadValue.SourceChannel.DescriptionText,
                    LeadIntent = leadValue.LeadIntent.DescriptionText,
                    LeadStage = leadValue.LeadStage.DescriptionText,
                    Priority = leadValue.Priority?.ToString() ?? "N/A",
                    Notes = leadValue.Notes ?? "No notes"
                }.ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                    | dropDown
                    | editBtn
        ).Title("Lead Details").Width(Size.Units(100));

        var relatedCard = new Card(
            new List(
                new ListItem("Call Records", onClick: _ =>
                {
                    blades.Push(this, new LeadCallRecordsBlade(leadId), "Call Records");
                }, badge: callRecordCount.Value.ToString("N0")),
                new ListItem("Media", onClick: _ =>
                {
                    blades.Push(this, new LeadMediaBlade(leadId), "Media");
                }, badge: mediaCount.Value.ToString("N0")),
                new ListItem("Messages", onClick: _ =>
                {
                    blades.Push(this, new LeadMessagesBlade(leadId), "Messages");
                }, badge: messageCount.Value.ToString("N0")),
                new ListItem("Tasks", onClick: _ =>
                {
                    blades.Push(this, new LeadTasksBlade(leadId), "Tasks");
                }, badge: taskCount.Value.ToString("N0"))
            ));

        return new Fragment()
               | (Layout.Vertical() | detailsCard | relatedCard)
               | alertView;
    }

    private void Delete(AutodealerCrmContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var lead = db.Leads.FirstOrDefault(e => e.Id == leadId)!;
        db.Leads.Remove(lead);
        db.SaveChanges();
    }
}