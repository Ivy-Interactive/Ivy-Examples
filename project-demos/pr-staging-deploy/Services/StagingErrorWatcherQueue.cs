namespace PrStagingDeploy.Services;

using System.Threading.Channels;

/// <summary>
/// After a successful service creation Sliplane builds asynchronously.
/// This queue lets a background worker detect build failures and update the PR comment.
/// </summary>
public class StagingErrorWatcherQueue
{
    public Channel<StagingErrorWatchRequest> Channel { get; } =
        System.Threading.Channels.Channel.CreateBounded<StagingErrorWatchRequest>(
            new BoundedChannelOptions(200)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = false,
                SingleWriter = false
            });

    public ValueTask EnqueueAsync(StagingErrorWatchRequest req, CancellationToken ct = default)
        => Channel.Writer.WriteAsync(req, ct);
}

public record StagingErrorWatchRequest(
    string RepoKey,
    string Owner,
    string Repo,
    int PrNumber,
    string? DocsServiceId,
    string? SamplesServiceId);
