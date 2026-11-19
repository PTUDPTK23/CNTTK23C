using System.Threading.Channels;

namespace EduSubmit.Api.Services;

public sealed class SubmissionQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(Guid submissionId)
    {
        if (!_channel.Writer.TryWrite(submissionId))
        {
            throw new InvalidOperationException("Could not queue the submission.");
        }
    }

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
