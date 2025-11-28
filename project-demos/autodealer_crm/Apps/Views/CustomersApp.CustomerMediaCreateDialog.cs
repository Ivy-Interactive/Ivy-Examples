namespace AutodealerCrm.Apps.Views;

public class CustomerMediaCreateDialog(IState<bool> isOpen, RefreshToken refreshToken, int? customerId) : ViewBase
{
    private record MediaCreateRequest
    {
        [Required]
        public string FilePath { get; init; } = "";

        [Required]
        public string FileType { get; init; } = "";
    }

    public override object? Build()
    {
        var factory = UseService<AutodealerCrmContextFactory>();
        var mediaRequest = UseState(() => new MediaCreateRequest());

        UseEffect(() =>
        {
            if (mediaRequest.Value.FilePath != null && mediaRequest.Value.FileType != null)
            {
                var mediaId = CreateMedia(factory, mediaRequest.Value);
                refreshToken.Refresh(mediaId);
            }
        }, [mediaRequest]);

        return mediaRequest
            .ToForm()
            .Builder(e => e.FilePath, e => e.ToTextInput())
            .Builder(e => e.FileType, e => e.ToTextInput())
            .ToDialog(isOpen, title: "Create Media", submitTitle: "Create");
    }

    private int CreateMedia(AutodealerCrmContextFactory factory, MediaCreateRequest request)
    {
        using var db = factory.CreateDbContext();

        var media = new Medium()
        {
            FilePath = request.FilePath,
            FileType = request.FileType,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O"),
            CustomerId = customerId 
        };

        db.Media.Add(media);
        db.SaveChanges();

        return media.Id;
    }
}