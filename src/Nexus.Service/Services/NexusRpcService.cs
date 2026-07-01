using Nexus.Service.Interfaces;
using Nexus.Shared.Interfaces;
using Nexus.Shared.Models;

namespace Nexus.Service.Services;

#pragma warning disable CA1812
internal sealed partial class NexusRpcService(
    ISystemCoordinator hardwareService,
    ILogger<NexusRpcService> logger) : INexusRpcService
{
    public async Task SetFanModeAsync(FanMode mode)
    {
        LogFanModeRequest(logger, mode);
        await hardwareService.SetFanModeAsync(mode).ConfigureAwait(false);
    }

    public async Task<bool> SetMuxStateAsync(MuxState targetMode)
        => await hardwareService.SetMuxStateAsync(targetMode).ConfigureAwait(false);

    public async Task ForceResetFanModeAsync()
        => await hardwareService.ForceResetFanModeAsync().ConfigureAwait(false);

    public Task<IReadOnlyList<FanMode>> GetSupportedFanModesAsync()
        => Task.FromResult(hardwareService.SupportedFanModes);

    public Task<IReadOnlyList<MuxState>> GetSupportedMuxModesAsync()
        => Task.FromResult(hardwareService.SupportedMuxModes);

    public Task<FanConfig> GetFanConfigAsync()
    {
        LogConfigRequested(logger);
        return Task.FromResult(hardwareService.CurrentFanConfig);
    }

    public async Task ApplyFanConfigAsync(FanConfig newConfig)
    {
        LogNewConfigReceived(logger);
        await hardwareService.ApplyFanConfigAsync(newConfig).ConfigureAwait(false);
    }

    public IAsyncEnumerable<SystemStats> StreamTelemetryAsync(CancellationToken cancellationToken)
    {
        LogStreamInitiated(logger);
        return hardwareService.StreamTelemetryAsync(cancellationToken);
    }

    public async Task<bool> RetryCoreTempConnectionAsync()
        => await hardwareService.TryConnectCoreTempAsync().ConfigureAwait(false);

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "IPC request received to set Fan Mode: {Mode}")]
    private static partial void LogFanModeRequest(ILogger logger, FanMode mode);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "IPC: Fan Configuration requested by the UI.")]
    private static partial void LogConfigRequested(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "IPC: New Fan Configuration submitted by the UI.")]
    private static partial void LogNewConfigReceived(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "IPC: New Telemetry stream initiated.")]
    private static partial void LogStreamInitiated(ILogger logger);
    #endregion
}