namespace AutodealerCrm.Apps.Views;

public class VehicleMediaCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int? vehicleId) : ViewBase
{
    private record MediaCreateRequest
    {
        [Required]
        public string FilePath { get; init; } = "";

        [Required]
        public string FileType { get; init; } = "";

        [Required]
        public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var media = UseState(() => new MediaCreateRequest());

        UseEffect(() =>
        {
            var mediaId = CreateMedia(factory, media.Value, vehicleId);
            refreshToken.Refresh(mediaId);
        }, [media]);

        return media
            .ToForm()
            .Builder(e => e.FilePath, e => e.ToTextInput())
            .Builder(e => e.FileType, e => e.ToTextInput())
            .Builder(e => e.UploadedAt, e => e.ToDateTimeInput())
            .ToDialog(isOpen, title: "Create Media", submitTitle: "Create");
    }

    private int CreateMedia(AutodealerCrmContextFactory factory, MediaCreateRequest request, int? vehicleId)
    {
        using var db = factory.CreateDbContext();

        var media = new Medium
        {
            FilePath = request.FilePath,
            FileType = request.FileType,
            UploadedAt = request.UploadedAt.ToString("O"),
            VehicleId = vehicleId,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        db.Media.Add(media);
        db.SaveChanges();

        return media.Id;
    }
}