using Nexus.Shared.Models;
using PolyType;
using StreamJsonRpc;

namespace Nexus.Shared.Interfaces;

[JsonRpcContract]
[GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
public partial interface INexusRpcService
{
    Task SetFanModeAsync(FanMode mode);
    Task<bool> SetMuxStateAsync(MuxState targetMode);
    Task<IReadOnlyList<MuxState>> GetSupportedMuxModesAsync();
    Task ForceResetFanModeAsync();
    IAsyncEnumerable<SystemStats> StreamTelemetryAsync(CancellationToken cancellationToken);
    Task<FanConfig> GetFanConfigAsync();
    Task ApplyFanConfigAsync(FanConfig newConfig);
    Task<bool> RetryCoreTempConnectionAsync();
}