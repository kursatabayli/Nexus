using Nexus.Service.Interfaces;
using Nexus.Shared.Models;

namespace Nexus.Service.Controllers;

#pragma warning disable CA1812
internal sealed partial class FanController : IFanController, IDisposable
{
    private readonly ILogger<FanController> _logger;
    private readonly IAcpiCmdService _acpiCmdService;
    private readonly Timer _watchdogTimer;
    private byte _maxCpuByte = 55;
    private byte _maxGpuByte = 55;
    private byte _lastCpuVal;
    private byte _lastGpuVal;
    private bool _disposed;
    private bool _isManualFanSupported;
    private bool _isMaxFanSupported;

    public FanController(
        ILogger<FanController> logger,
        IAcpiCmdService acpiCmdService)
    {
        _logger = logger;
        _acpiCmdService = acpiCmdService;

        _watchdogTimer = new Timer(WatchdogCallback, null, Timeout.Infinite, Timeout.Infinite);
    }


    public FanMode CurrentMode { get; private set; } = FanMode.Auto;

    public async Task InitializeAsync(bool isManualFanSupported, bool isMaxFanSupported)
    {
        LogInitializingLimits(_logger);

        _isManualFanSupported = isManualFanSupported;
        _isMaxFanSupported = isMaxFanSupported;

        if (_isManualFanSupported)
            await InitializeFanLimitsAsync().ConfigureAwait(false);
        else
            LogHardwareReject(_logger, "Manuel Fan Desteklenmiyor. Limitler okunmayacak.");
    }

    public async Task SetFanModeAsync(FanMode mode)
    {
        if (mode == FanMode.Max && !_isMaxFanSupported)
        {
            LogHardwareReject(_logger, "Max Fan Mode");
            return;
        }
        if (mode == FanMode.Manual && !_isManualFanSupported)
        {
            LogHardwareReject(_logger, "Manual Fan Mode");
            return;
        }

        CurrentMode = mode;
        LogChangingMode(_logger, mode);

        await _acpiCmdService.SetThermalProfileSetupAsync().ConfigureAwait(false);

        switch (mode)
        {
            case FanMode.Max:
                await _acpiCmdService.SetMaxFanSpeedAsync(true).ConfigureAwait(false);
                _watchdogTimer.Change(TimeSpan.FromSeconds(90), TimeSpan.FromSeconds(90));
                break;

            case FanMode.Auto:
                await _acpiCmdService.SetMaxFanSpeedAsync(false).ConfigureAwait(false);
                await _acpiCmdService.SetFanSpeedAsync(0, 0).ConfigureAwait(false);

                _lastCpuVal = 0;
                _lastGpuVal = 0;
                _watchdogTimer.Change(Timeout.Infinite, Timeout.Infinite);
                break;

            case FanMode.Manual:
                await _acpiCmdService.SetMaxFanSpeedAsync(false).ConfigureAwait(false);
                _watchdogTimer.Change(Timeout.Infinite, Timeout.Infinite);
                break;
        }
    }

    public async Task SetFanSpeedAsync(int cpuPercentage, int gpuPercentage)
    {
        cpuPercentage = Math.Clamp(cpuPercentage, 0, 100);
        gpuPercentage = Math.Clamp(gpuPercentage, 0, 100);

        _lastCpuVal = (byte)(cpuPercentage == 0 ? 0 : (cpuPercentage * _maxCpuByte / 100));
        _lastGpuVal = (byte)(gpuPercentage == 0 ? 0 : (gpuPercentage * _maxGpuByte / 100));

        await _acpiCmdService.SetThermalProfileSetupAsync().ConfigureAwait(false);
        await _acpiCmdService.SetFanSpeedAsync(_lastCpuVal, _lastGpuVal).ConfigureAwait(false);
    }

    public async Task<(int CpuRpm, int GpuRpm)> ReadFanRpmsAsync()
    {
        var rpmResult = await _acpiCmdService.GetFanRpmsAsync().ConfigureAwait(false);

        if (rpmResult.Success && rpmResult.ReturnData != null && rpmResult.ReturnData.Length >= 2)
            return (rpmResult.ReturnData[0] * 100, rpmResult.ReturnData[1] * 100);

        return (0, 0);
    }

    private async Task InitializeFanLimitsAsync()
    {
        var tableResult = await _acpiCmdService.GetFanTableAsync().ConfigureAwait(false);

        if (tableResult.Success && tableResult.ReturnData != null && tableResult.ReturnData.Length > 2)
        {
            ReadOnlySpan<byte> t = tableResult.ReturnData;

            for (int i = 2; i < t.Length - 2; i += 3)
            {
                byte cpu = t[i];
                byte gpu = t[i + 1];

                if (cpu == 0 && gpu == 0) break;

                if (cpu > _maxCpuByte) _maxCpuByte = cpu;
                if (gpu > _maxGpuByte) _maxGpuByte = gpu;
            }

            LogLimitsInitialized(_logger, _maxCpuByte, _maxGpuByte);
        }
        else
        {
            LogLimitsFailed(_logger);
        }
    }

    private void WatchdogCallback(object? state)
    {
        if (CurrentMode == FanMode.Max)
        {
            LogWatchdogTriggered(_logger);
            _ = Task.Run(() => SetFanModeAsync(FanMode.Max));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _watchdogTimer.Dispose();
        _disposed = true;
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "ACPI: Reading fan limit table from hardware...")]
    private static partial void LogInitializingLimits(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "ACPI: Fan limits successfully calculated. (Max CPU Byte: {CpuByte}, Max GPU Byte: {GpuByte})")]
    private static partial void LogLimitsInitialized(ILogger logger, byte cpuByte, byte gpuByte);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "ACPI: Failed to read fan table limits! Using safe defaults (55).")]
    private static partial void LogLimitsFailed(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "WatchDog Triggered: Refreshing Max fan mode to prevent BIOS override...")]
    private static partial void LogWatchdogTriggered(ILogger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Hardware: Applying Fan Mode '{Mode}' via ACPI...")]
    private static partial void LogChangingMode(ILogger logger, FanMode mode);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Hardware Reject: The requested feature '{Feature}' is not supported by the system.")]
    private static partial void LogHardwareReject(ILogger logger, string feature);
    #endregion
}