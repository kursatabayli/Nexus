using Nexus.Service.Hubs;
using Nexus.Service.Interfaces;
using Nexus.Service.Services;
using Nexus.Shared.Helpers;
using Nexus.Shared.Models;
using static Nexus.Service.Consts.HpWmiConstants;

namespace Nexus.Service.Controllers;

#pragma warning disable CA1812
internal sealed partial class FanController : IFanController, IDisposable
{
    private readonly ILogger<FanController> _logger;
    private readonly IAcpiService _acpiService;
    private readonly Timer _watchdogTimer;
    private byte _maxCpuByte = 55;
    private byte _maxGpuByte = 55;
    private byte _lastCpuVal;
    private byte _lastGpuVal;
    private bool _disposed;

    public FanController(
        ILogger<FanController> logger,
        IAcpiService acpiService)
    {
        _logger = logger;
        _acpiService = acpiService;

        _watchdogTimer = new Timer(WatchdogCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    public FanMode CurrentMode { get; private set; } = FanMode.Auto;

    public async Task InitializeAsync()
    {
        LogInitializingLimits(_logger);

        var tableResult = await _acpiService.ExecuteAsync(Operation.GameManager, Feature.VictusSGetFanTableQuery, [0, 0, 0, 0], BufferSize128).ConfigureAwait(false);

        if (tableResult.Success && tableResult.ReturnData != null && tableResult.ReturnData.Length > 2)
        {
            byte[] t = tableResult.ReturnData;
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

    public async Task SetFanModeAsync(FanMode mode)
    {
        CurrentMode = mode;
        LogChangingMode(_logger, mode);

        await _acpiService.ExecuteAsync(Operation.GameManager, Feature.ThermalProfileSetup, [0, 0, 0, 0], BufferSize4).ConfigureAwait(false);

        switch (mode)
        {
            case FanMode.Max:
                await _acpiService.ExecuteAsync(Operation.GameManager, Feature.FanSpeedMaxSetQuery, [PayloadMaxFanEnable, 0, 0, 0], BufferSize0).ConfigureAwait(false);
                _watchdogTimer.Change(TimeSpan.FromSeconds(90), TimeSpan.FromSeconds(90));
                break;

            case FanMode.Auto:
                await _acpiService.ExecuteAsync(Operation.GameManager, Feature.FanSpeedMaxSetQuery, [PayloadMaxFanDisable, 0, 0, 0], BufferSize0).ConfigureAwait(false);
                await _acpiService.ExecuteAsync(Operation.GameManager, Feature.VictusSFanSpeedSetQuery, [0, 0, 0, 0], BufferSize0).ConfigureAwait(false);

                _lastCpuVal = 0;
                _lastGpuVal = 0;
                _watchdogTimer.Change(Timeout.Infinite, Timeout.Infinite);
                break;

            case FanMode.Manual:
                await _acpiService.ExecuteAsync(Operation.GameManager, Feature.FanSpeedMaxSetQuery, [PayloadMaxFanDisable, 0, 0, 0], BufferSize0).ConfigureAwait(false);
                _watchdogTimer.Change(Timeout.Infinite, Timeout.Infinite);
                break;
        }
    }

    private void WatchdogCallback(object? state)
    {
        if (CurrentMode == FanMode.Max)
        {
            _ = SetFanModeAsync(FanMode.Max);
        }
    }

    public async Task SetFanSpeedAsync(int cpuPercentage, int gpuPercentage)
    {
        cpuPercentage = Math.Clamp(cpuPercentage, 0, 100);
        gpuPercentage = Math.Clamp(gpuPercentage, 0, 100);

        _lastCpuVal = (byte)(cpuPercentage == 0 ? 0 : (cpuPercentage * _maxCpuByte / 100));
        _lastGpuVal = (byte)(gpuPercentage == 0 ? 0 : (gpuPercentage * _maxGpuByte / 100));

        await _acpiService.ExecuteAsync(Operation.GameManager, Feature.ThermalProfileSetup, [0, 0, 0, 0], BufferSize4).ConfigureAwait(false);
        await _acpiService.ExecuteAsync(Operation.GameManager, Feature.VictusSFanSpeedSetQuery, [_lastCpuVal, _lastGpuVal, 0, 0], BufferSize0).ConfigureAwait(false);
    }

    public async Task<(int CpuRpm, int GpuRpm)> ReadFanRpmsAsync()
    {
        var rpmResult = await _acpiService.ExecuteAsync(Operation.GameManager, Feature.VictusSFanSpeedGetQuery, [0, 0, 0, 0], BufferSize128).ConfigureAwait(false);

        if (rpmResult.Success && rpmResult.ReturnData != null && rpmResult.ReturnData.Length >= 2)
        {
            return (rpmResult.ReturnData[0] * 100, rpmResult.ReturnData[1] * 100);
        }

        return (0, 0);
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
    #endregion
}