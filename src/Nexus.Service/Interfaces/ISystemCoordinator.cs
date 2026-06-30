using Nexus.Shared.Models;

namespace Nexus.Service.Interfaces;

internal interface ISystemCoordinator
{
    FanConfig CurrentFanConfig { get; }
    IReadOnlyList<MuxState> SupportedMuxModes { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<SystemStats> GetSystemStatsAsync(bool isUiConnected);
    Task SetFanModeAsync(FanMode mode);
    Task<bool> SetMuxStateAsync(MuxState targetMode);
    Task ApplyFanConfigAsync(FanConfig newConfig);
    Task ForceResetFanModeAsync();
    Task<bool> TryConnectCoreTempAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<SystemStats> StreamTelemetryAsync(CancellationToken cancellationToken);
}