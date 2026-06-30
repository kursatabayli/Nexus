using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Nexus.Shared.Models;

namespace Nexus.Service.Hubs;

internal interface ITelemetryHub
{
    bool HasActiveUiClients { get; }
    event Action<SystemStats>? OnInternalStatsUpdated;
    void Publish(SystemStats stats);
    IAsyncEnumerable<SystemStats> SubscribeAsync(CancellationToken cancellationToken);
}

#pragma warning disable CA1812
internal sealed class TelemetryHub : ITelemetryHub
{
    private int _uiClientCount;
    public bool HasActiveUiClients => Volatile.Read(ref _uiClientCount) > 0;
    public event Action<SystemStats>? OnInternalStatsUpdated;

    public void Publish(SystemStats stats)
    {
        OnInternalStatsUpdated?.Invoke(stats);
    }

    public async IAsyncEnumerable<SystemStats> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _uiClientCount);

        var channelOptions = new BoundedChannelOptions(5) 
        { 
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true 
        };

        var localChannel = Channel.CreateBounded<SystemStats>(channelOptions);

        void Handler(SystemStats s) => localChannel.Writer.TryWrite(s);
        OnInternalStatsUpdated += Handler;

        try
        {
            await foreach (var stat in localChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return stat;
            }
        }
        finally
        {
            OnInternalStatsUpdated -= Handler;
            Interlocked.Decrement(ref _uiClientCount);
        }
    }
}