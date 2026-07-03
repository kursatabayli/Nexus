using Nexus.Service.Helpers;
using Nexus.Service.Hubs;
using Nexus.Service.Interfaces;
using Nexus.Shared.Helpers;
using Nexus.Shared.Models;

namespace Nexus.Service.Services;

#pragma warning disable CA1812
internal sealed partial class SystemCoordinator : ISystemCoordinator, IDisposable
{
    private readonly ILogger<SystemCoordinator> _logger;
    private readonly ILogger<CoreTempMonitor> _coreTempLogger;
    private readonly IFanController _fanController;
    private readonly IMuxController _muxController;
    private readonly IConfigService _configService;
    private readonly ITelemetryHub _telemetryHub;
    private readonly IPlatformSupportService _platformSupportService;

    private uint _nvidiaAdapterHandle;
    private CoreTempMonitor? _coreTempMonitor;
    private bool _isCoreTempConnected;
    private bool _isInitialized;
    private bool _disposed;
    private FanConfig _currentConfig;
    private FanMode[] _supportedFanModes = [FanMode.Auto];

    public SystemCoordinator(
        ILogger<SystemCoordinator> logger,
        ILogger<CoreTempMonitor> coreTempLogger,
        IFanController fanController,
        IMuxController muxController,
        IConfigService configService,
        ITelemetryHub telemetryHub,
        IPlatformSupportService platformSupportService)
    {
        _logger = logger;
        _coreTempLogger = coreTempLogger;
        _fanController = fanController;
        _muxController = muxController;
        _configService = configService;
        _telemetryHub = telemetryHub;
        _platformSupportService = platformSupportService;

        _currentConfig = _configService.Load() ?? FanConfig.Default;

        _telemetryHub.OnInternalStatsUpdated += HandleTelemetryUpdate;
    }

    public FanConfig CurrentFanConfig => _currentConfig;
    public IReadOnlyList<MuxState> SupportedMuxModes => _muxController.SupportedModes;
    public IReadOnlyList<FanMode> SupportedFanModes => _supportedFanModes;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        try
        {
            var (isManualFanSupported, isMaxFanSupported) = await _platformSupportService.InitializeAsync().ConfigureAwait(false);

            _supportedFanModes = (isManualFanSupported, isMaxFanSupported) switch
            {
                (true, true) => [FanMode.Auto, FanMode.Manual, FanMode.Max],
                (true, false) => [FanMode.Auto, FanMode.Manual],
                (false, true) => [FanMode.Auto, FanMode.Max],
                (false, false) => [FanMode.Auto]
            };

            await _fanController.InitializeAsync(isManualFanSupported, isMaxFanSupported).ConfigureAwait(false);
            await _muxController.InitializeAsync().ConfigureAwait(false);

            LogInitialMuxStateCached(_logger, _muxController.CurrentMuxState);

            _nvidiaAdapterHandle = WddmTelemetryProvider.InitializeNvidiaAdapterHandle(_logger);

            if (_nvidiaAdapterHandle != 0)
                LogNvidiaHooked(_logger, _nvidiaAdapterHandle);
            else
                LogNvidiaNotFound(_logger);

            await TryConnectCoreTempAsync(cancellationToken).ConfigureAwait(false);

            var initialMode = _currentConfig.LastMode;

            if (initialMode == FanMode.Manual && !_isCoreTempConnected)
            {
                LogCoreTempMissingFallback(_logger);
                initialMode = FanMode.Auto;
            }

            if (!_supportedFanModes.Contains(initialMode))
                initialMode = FanMode.Auto;

            await _fanController.SetFanModeAsync(initialMode).ConfigureAwait(false);

            _isInitialized = true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<SystemStats> GetSystemStatsAsync(bool isUiConnected)
    {
        bool includeRpm = isUiConnected;
        bool includeTemp = isUiConnected || _fanController.CurrentMode == FanMode.Manual;

        var (CpuRpm, GpuRpm) = includeRpm ? await _fanController.ReadFanRpmsAsync().ConfigureAwait(false) : (0, 0);
        var (CpuTemp, GpuTemp) = includeTemp ? ReadTemperatures() : (0, 0);

        return new SystemStats(
            CpuTemp: CpuTemp,
            GpuTemp: GpuTemp,
            CpuFanRpm: CpuRpm,
            GpuFanRpm: GpuRpm,
            CurrentMode: _fanController.CurrentMode,
            CurrentMuxState: _muxController.CurrentMuxState
        );
    }

    public async Task SetFanModeAsync(FanMode mode)
        => await _fanController.SetFanModeAsync(mode).ConfigureAwait(false);
    public async Task ForceResetFanModeAsync()
        => await _fanController.SetFanModeAsync(FanMode.Auto).ConfigureAwait(false);
    public async Task<bool> SetMuxStateAsync(MuxState targetMode)
        => await _muxController.ChangeMuxMode(targetMode).ConfigureAwait(false);

    public async Task ApplyFanConfigAsync(FanConfig newConfig)
    {
        _currentConfig = newConfig;
        _configService.Save(_currentConfig);
        await _fanController.SetFanModeAsync(newConfig.LastMode).ConfigureAwait(false);
    }

    public async Task<bool> TryConnectCoreTempAsync(CancellationToken cancellationToken = default)
    {
        if (_isCoreTempConnected) return true;

        try
        {
            CoreTempManager.EnsureCoreTempReady(_logger);
            _coreTempMonitor = new CoreTempMonitor(_coreTempLogger);
            _isCoreTempConnected = await _coreTempMonitor.InitializeAsync(10, 500, cancellationToken).ConfigureAwait(false);

            return _isCoreTempConnected;
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException or UnauthorizedAccessException or InvalidOperationException or IOException)
        {
            return false;
        }
    }

    public IAsyncEnumerable<SystemStats> StreamTelemetryAsync(CancellationToken cancellationToken)
        => _telemetryHub.SubscribeAsync(cancellationToken);

    private async void HandleTelemetryUpdate(SystemStats stats)
    {
        if (_fanController.CurrentMode != FanMode.Manual) return;

        ReadOnlySpan<FanCurvePoint> cpuSpan = [.. _currentConfig.CpuCurve];
        ReadOnlySpan<FanCurvePoint> gpuSpan = [.. _currentConfig.GpuCurve];

        int targetCpu = FanCurveCalculator.CalculatePercentage(stats.CpuTemp, cpuSpan);
        int targetGpu = FanCurveCalculator.CalculatePercentage(stats.GpuTemp, gpuSpan);

        await _fanController.SetFanSpeedAsync(targetCpu, targetGpu).ConfigureAwait(false);
    }

    private (int CpuTemp, int GpuTemp) ReadTemperatures()
    {
        int rawCpuTemp = _isCoreTempConnected && _coreTempMonitor != null ? _coreTempMonitor.GetMaxCpuTemperature() : -1;

        int gpuTemp = 0;
        if (_nvidiaAdapterHandle != 0)
        {
            var wddmTemp = WddmTelemetryProvider.GetGpuTemperature(_nvidiaAdapterHandle, _logger);
            if (wddmTemp > 0)
                gpuTemp = (int)Math.Round(wddmTemp);
        }
        return (rawCpuTemp, gpuTemp);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _coreTempMonitor?.Dispose();
        _disposed = true;
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Initial MUX State Cached: {MuxState}")]
    private static partial void LogInitialMuxStateCached(ILogger logger, MuxState muxState);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "WDDM: Discrete NVIDIA Card successfully hooked. (Handle: {Handle})")]
    private static partial void LogNvidiaHooked(ILogger logger, uint handle);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "WDDM: NVIDIA card not found in the system. Fallback ACPI data will be used.")]
    private static partial void LogNvidiaNotFound(ILogger logger);
    
    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "CoreTemp connection failed. Falling back to Auto mode instead of Manual to prevent overheating.")]
    private static partial void LogCoreTempMissingFallback(ILogger logger);
    #endregion
}