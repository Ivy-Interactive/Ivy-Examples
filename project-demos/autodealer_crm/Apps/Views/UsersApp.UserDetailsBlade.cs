namespace AutodealerCrm.Apps.Views;

public class UserDetailsBlade(int userId) : ViewBase
{
    public override object? Build()
    {
        var factory = this.UseService<AutodealerCrmContextFactory>();
        var blades = this.UseContext<IBladeController>();
        var refreshToken = this.UseRefreshToken(); 
        var user = this.UseState<User?>();
        var callRecordCount = this.UseState<int>();
        var leadCount = this.UseState<int>();
        var messageCount = this.UseState<int>();
        var taskCount = this.UseState<int>();
        var vehicleCount = this.UseState<int>();
        var (alertView, showAlert) = this.UseAlert();

        this.UseEffect(async () =>
        {
            var db = factory.CreateDbContext();
            user.Set(await db.Users
                .Include(e => e.UserRole)
                .SingleOrDefaultAsync(e => e.Id == userId));
            callRecordCount.Set(await db.CallRecords.CountAsync(e => e.ManagerId == userId));
            leadCount.Set(await db.Leads.CountAsync(e => e.ManagerId == userId));
            messageCount.Set(await db.Messages.CountAsync(e => e.ManagerId == userId));
            taskCount.Set(await db.Tasks.CountAsync(e => e.ManagerId == userId));
            vehicleCount.Set(await db.Vehicles.CountAsync(e => e.ManagerId == userId));
        }, [EffectTrigger.AfterInit(), refreshToken]);

        if (user.Value == null) return null;

        var userValue = user.Value;

        void OnDelete()
        {
            showAlert("Are you sure you want to delete this user?", result =>
            {
                if (result.IsOk())
                {
                    Delete(factory);
                    blades.Pop(refresh: true);
                }
            }, "Delete User", AlertButtonSet.OkCancel);
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
            .ToTrigger((isOpen) => new UserEditSheet(isOpen, refreshToken, userId));

        var detailsCard = new Card(
            content: new
                {
                    userValue.Id,
                    FullName = $"{userValue.Name}",
                    userValue.Email,
                    UserRole = userValue.UserRole.DescriptionText
                }.ToDetails()
                .RemoveEmpty()
                .Builder(e => e.Id, e => e.CopyToClipboard()),
            footer: Layout.Horizontal().Gap(2).Align(Align.Right)
                    | dropDown
                    | editBtn
        ).Title("User Details").Width(Size.Units(100));
        
        var relatedCard = new Card(
            new List(
                new ListItem("Call Records", onClick: _ =>
                {
                    blades.Push(this, new UserCallRecordsBlade(userId), "Call Records");
                }, badge: callRecordCount.Value.ToString("N0")),
                new ListItem("Leads", onClick: _ =>
                {
                    blades.Push(this, new UserLeadsBlade(userId), "Leads");
                }, badge: leadCount.Value.ToString("N0")),
                new ListItem("Messages", onClick: _ =>
                {
                    blades.Push(this, new UserMessagesBlade(userId), "Messages");
                }, badge: messageCount.Value.ToString("N0")),
                new ListItem("Tasks", onClick: _ =>
                {
                    blades.Push(this, new UserTasksBlade(userId), "Tasks");
                }, badge: taskCount.Value.ToString("N0")),
                new ListItem("Vehicles", onClick: _ =>
                {
                    blades.Push(this, new UserVehiclesBlade(userId), "Vehicles");
                }, badge: vehicleCount.Value.ToString("N0"))
            ));

        return new Fragment() 
               | (Layout.Vertical() | detailsCard | relatedCard)
               | alertView;
    }

    private void Delete(AutodealerCrmContextFactory dbFactory)
    {
        using var db = dbFactory.CreateDbContext();
        var user = db.Users.FirstOrDefault(e => e.Id == userId)!;
        db.Users.Remove(user);
        db.SaveChanges();
    }
}