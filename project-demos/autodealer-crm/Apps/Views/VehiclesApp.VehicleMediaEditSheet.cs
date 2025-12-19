namespace AutodealerCrm.Apps.Views;

public class VehicleMediaEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int mediaId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var media = UseState(() => factory.CreateDbContext().Media.FirstOrDefault(e => e.Id == mediaId)!);

        UseEffect(() =>
        {
            using var db = factory.CreateDbContext();
            media.Value.UpdatedAt = DateTime.UtcNow.ToString("O");
            db.Media.Update(media.Value);
            db.SaveChanges();
            refreshToken.Refresh();
        }, [media]);

        return media
            .ToForm()
            .Builder(e => e.FilePath, e => e.ToTextInput())
            .Builder(e => e.FileType, e => e.ToTextInput())
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Media");
    }
}