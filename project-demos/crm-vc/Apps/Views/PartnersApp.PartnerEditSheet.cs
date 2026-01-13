namespace Vc.Apps.Views;

public class PartnerEditSheet(IState<bool> isOpen, RefreshToken refreshToken, int partnerId) : ViewBase
{
    public override object? Build()
    {
        var factory = UseService<VcContextFactory>();
        var partner = UseState(() => factory.CreateDbContext().Partners.FirstOrDefault(e => e.Id == partnerId)!);
        var client = UseService<IClientProvider>();

        UseEffect(() =>
        {
            try
            {
                using var db = factory.CreateDbContext();
                partner.Value.UpdatedAt = DateTime.UtcNow;
                db.Partners.Update(partner.Value);
                db.SaveChanges();
                refreshToken.Refresh();
            }
            catch (Exception ex)
            {
                client.Toast(ex);
            }
        }, [partner]);

        return partner
            .ToForm()
            .Builder(e => e.Email, e => e.ToEmailInput())
            .Place(e => e.FirstName, e => e.LastName)
            .Remove(e => e.Id, e => e.CreatedAt, e => e.UpdatedAt)
            .Builder(e => e.GenderId, e => e.ToAsyncSelectInput<int?>(QueryGenders, LookupGender, placeholder: "Select Gender"))
            .ToSheet(isOpen, "Edit Partner");
    }

    private static QueryResult<Option<int?>[]> QueryGenders(IViewContext context, string query)
    {
        var factory = context.UseService<VcContextFactory>();
        return context.UseQuery<Option<int?>[], (string, string)>(
            key: (nameof(QueryGenders), query),
            fetcher: async ct =>
            {
                await using var db = factory.CreateDbContext();
                return (await db.Genders
                        .Where(e => e.DescriptionText.Contains(query))
                        .Select(e => new { e.Id, e.DescriptionText })
                        .Take(50)
                        .ToArrayAsync(ct))
                    .Select(e => new Option<int?>(e.DescriptionText, e.Id))
                    .ToArray();
            });
    }

    private static QueryResult<Option<int?>?> LookupGender(IViewContext context, int? id)
    {
        var factory = context.UseService<VcContextFactory>();
        return context.UseQuery<Option<int?>?, (string, int?)>(
            key: (nameof(LookupGender), id),
            fetcher: async ct =>
            {
                if (id == null) return null;
                await using var db = factory.CreateDbContext();
                var gender = await db.Genders.FirstOrDefaultAsync(e => e.Id == id, ct);
                if (gender == null) return null;
                return new Option<int?>(gender.DescriptionText, gender.Id);
            });
    }
}