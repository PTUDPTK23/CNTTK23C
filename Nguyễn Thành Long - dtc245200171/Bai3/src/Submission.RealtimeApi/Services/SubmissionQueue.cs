using System.Threading.Channels;

namespace Submission.Api.Services;

public sealed class SubmissionQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    public bool Enqueue(Guid submissionId) => _channel.Writer.TryWrite(submissionId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
