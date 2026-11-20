using System.Threading.Channels;

namespace MovieSearchCase.WebApi.Application.Logging;

public sealed class ErrorLogQueue
{
    private readonly Channel<string> _entries = Channel.CreateBounded<string>(
        new BoundedChannelOptions(ApiDefaults.ErrorLogQueueCapacity)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false,
    });

    private long _droppedEntries;

    public long DroppedEntries => Interlocked.Read(ref _droppedEntries);

    public bool TryEnqueue(string entry)
    {
        if (_entries.Writer.TryWrite(entry))
        {
            return true;
        }

        Interlocked.Increment(ref _droppedEntries);
        return false;
    }

    public IAsyncEnumerable<string> ReadAllAsync(CancellationToken cancellationToken) =>
        _entries.Reader.ReadAllAsync(cancellationToken);
}
