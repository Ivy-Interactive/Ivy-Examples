namespace AutodealerCrm.Apps.Views;

public class LeadMediaCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int? leadId) : ViewBase
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
        var mediaState = UseState(() => new MediaCreateRequest());

        UseEffect(() =>
        {
            if (leadId.HasValue)
            {
                var mediaId = CreateMedia(factory, mediaState.Value, leadId.Value);
                refreshToken.Refresh(mediaId);
            }
        }, [mediaState]);

        return mediaState
            .ToForm()
            .Builder(e => e.FilePath, e => e.ToTextInput())
            .Builder(e => e.FileType, e => e.ToTextInput())
            .Builder(e => e.UploadedAt, e => e.ToDateTimeInput())
            .ToDialog(isOpen, title: "Create Media", submitTitle: "Create");
    }

    private int CreateMedia(AutodealerCrmContextFactory factory, MediaCreateRequest request, int leadId)
    {
        using var db = factory.CreateDbContext();

        var media = new Medium
        {
            FilePath = request.FilePath,
            FileType = request.FileType,
            UploadedAt = request.UploadedAt.ToString("O"),
            LeadId = leadId,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        db.Media.Add(media);
        db.SaveChanges();

        return media.Id;
    }
}