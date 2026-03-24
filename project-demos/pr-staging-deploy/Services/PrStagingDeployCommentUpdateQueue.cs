namespace PrStagingDeploy.Services;

using System.Threading.Channels;

public class PrStagingDeployCommentUpdateQueue
{
    public Channel<PrStagingDeployCommentUpdateRequest> Channel { get; }

    public PrStagingDeployCommentUpdateQueue()
    {
        // Bounded to avoid runaway tasks; if full, we drop the oldest.
        var opts = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };
        Channel = System.Threading.Channels.Channel.CreateBounded<PrStagingDeployCommentUpdateRequest>(opts);
    }

    public ValueTask EnqueueAsync(PrStagingDeployCommentUpdateRequest request, CancellationToken cancellationToken = default)
        => Channel.Writer.WriteAsync(request, cancellationToken);
}

public record PrStagingDeployCommentUpdateRequest(
    string Owner,
    string Repo,
    int PrNumber,
    string BranchName,
    string? DocsServiceId,
    string? SamplesServiceId);

