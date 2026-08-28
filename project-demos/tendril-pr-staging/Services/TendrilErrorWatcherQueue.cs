namespace TendrilPrStaging.Services;

using System.Threading.Channels;

public class TendrilErrorWatcherQueue
{
    public Channel<TendrilErrorWatchRequest> Channel { get; } =
        System.Threading.Channels.Channel.CreateBounded<TendrilErrorWatchRequest>(
            new BoundedChannelOptions(200)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = false,
                SingleWriter = false
            });

    public ValueTask EnqueueAsync(TendrilErrorWatchRequest req, CancellationToken ct = default)
        => Channel.Writer.WriteAsync(req, ct);
}

public record TendrilErrorWatchRequest(
    string RepoKey,
    string Owner,
    string Repo,
    int PrNumber,
    string ServiceId,
    string? Branch = null);
