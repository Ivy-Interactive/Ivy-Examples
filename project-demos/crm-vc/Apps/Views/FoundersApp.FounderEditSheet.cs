namespace Vc.Apps.Views;

public class FounderEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int founderId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var founder = UseState(() => factory.CreateDbContext().Founders.FirstOrDefault(e => e.Id == founderId)!);
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                using var db = factory.CreateDbContext();
                founder.Value.UpdatedAt = DateTime.UtcNow;
                db.Founders.Update(founder.Value);
                db.SaveChanges();
                refreshToken.Refresh();
            }
            catch (Exception ex)
            {
                client.Toast(ex);
            }
        }, [founder]);

        return founder
            .ToForm()
            .Place(e => e.FirstName, e => e.LastName)
            .Builder(e => e.Email, e => e.ToEmailInput())
            .Builder(e => e.GenderId, e => e.ToAsyncSelectInput(QueryGenders(factory), LookupGender(factory), placeholder: "Select Gender"))
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .ToSheet(isOpen, "Edit Founder");
    }

    private static AsyncSelectQueryDelegate<int?> QueryGenders(VcContextFactory factory)
    {
        return async query =>
        {
            await using var db = factory.CreateDbContext();
            return (await db.Genders
                    .Where(e => e.DescriptionText.Contains(query))
                    .Select(e => new { e.Id, e.DescriptionText })
                    .Take(50)
                    .ToArrayAsync())
                .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                .ToArray();
        };
    }

    private static AsyncSelectLookupDelegate<int?> LookupGender(VcContextFactory factory)
    {
        return async id =>
        {
            if (id == null) return null;
            await using var db = factory.CreateDbContext();
            var gender = await db.Genders.FirstOrDefaultAsync(e => e.Id == id);
            if (gender == null) return null;
            return new Option<int?>(gender.DescriptionText, gender.Id);
        };
    }
}