using Nexus.Service.Interfaces;
using Nexus.Shared.Models;
using System.Collections.ObjectModel;
using static Nexus.Service.Consts.HpWmiConstants;

namespace Nexus.Service.Controllers;

#pragma warning disable CA1812
internal sealed partial class MuxController : IMuxController
{
    private readonly ILogger<MuxController> _logger;
    private readonly IAcpiCmdService _acpiCmdService;

    public MuxController(ILogger<MuxController> logger, IAcpiCmdService acpiCmdService)
    {
        _logger = logger;
        _acpiCmdService = acpiCmdService;
    }

    public MuxState CurrentMuxState { get; private set; } = MuxState.Undefined;
    public IReadOnlyList<MuxState> SupportedModes { get; private set; } = [];
    public bool IsLegacyMux { get; private set; }

    public async Task InitializeAsync()
    {
        await GetSupportedModesAsync().ConfigureAwait(false);

        var muxResult = await _acpiCmdService.ReadMuxStateAsync().ConfigureAwait(false);
        if (muxResult.Success && muxResult.ReturnCode != 3 && muxResult.ReturnCode != 4 && muxResult.ReturnData != null && muxResult.ReturnData.Length > 0)
        {
            sbyte state = (sbyte)(muxResult.ReturnData[0] & MuxModeMask);
            CurrentMuxState = state switch
            {
                0 => MuxState.Hybrid,
                1 => MuxState.Discrete,
                2 => MuxState.Optimus,
                3 => MuxState.UMA,
                _ => MuxState.Undefined
            };
        }
    }

    public async Task<bool> ChangeMuxMode(MuxState targetMode)
    {
        if (targetMode < MuxState.Hybrid || targetMode > MuxState.UMA)
        {
            LogInvalidMuxMode(_logger, targetMode);
            return false;
        }

        if (!SupportedModes.Contains(targetMode))
        {
            LogUnsupportedMuxModeRequested(_logger, targetMode);
            return false;
        }

        if (CurrentMuxState == targetMode)
        {
            LogMuxAlreadyInMode(_logger, targetMode);
            return true;
        }

        LogMuxChangeRequest(_logger, targetMode);

        var muxWrite = await _acpiCmdService.WriteMuxStateAsync((byte)targetMode).ConfigureAwait(false);

        if (muxWrite.Success && muxWrite.ReturnCode != 3 && muxWrite.ReturnCode != 4)
        {
            CurrentMuxState = targetMode;
            LogMuxChangeSuccess(_logger);
            return true;
        }
        else
        {
            LogMuxChangeFailed(_logger);
            return false;
        }
    }

    private async Task GetSupportedModesAsync()
    {
        var result = await _acpiCmdService.GetSystemDesignDataAsync().ConfigureAwait(false);

        if (result.Success && result.ReturnCode != 3 && result.ReturnCode != 4 && result.ReturnData?.Length > 7)
        {
            byte rawMask = result.ReturnData[7];
            SupportedModes = ParseSupportedModes(rawMask);
            IsLegacyMux = false;
            LogSupportedModesRead(_logger, "Modern", rawMask);
            return;
        }

        LogLegacyFallbackAttempt(_logger);
        var legacyResult = await _acpiCmdService.ReadMuxStateAsync().ConfigureAwait(false);

        if (legacyResult.Success && legacyResult.ReturnCode != 3 && legacyResult.ReturnCode != 4)
        {
            SupportedModes = ParseSupportedModes(MuxLegacyMask);
            IsLegacyMux = true;
            LogSupportedModesRead(_logger, "Legacy", MuxLegacyMask);
        }
        else
        {
            SupportedModes = [];
            LogMuxNotSupported(_logger);
        }
    }

    private static ReadOnlyCollection<MuxState> ParseSupportedModes(byte mask)
    {
        var list = new List<MuxState>();
        if ((mask & MuxModeHybrid) != 0) list.Add(MuxState.Hybrid);
        if ((mask & MuxModeDiscrete) != 0) list.Add(MuxState.Discrete);
        if ((mask & MuxModeOptimus) != 0) list.Add(MuxState.Optimus);
        if ((mask & MuxModeUma) != 0) list.Add(MuxState.UMA);
        return list.AsReadOnly();
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Received an invalid MUX mode request: {Mode}")]
    private static partial void LogInvalidMuxMode(ILogger logger, MuxState mode);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "The MUX switch is already operating in the requested mode: {Mode}")]
    private static partial void LogMuxAlreadyInMode(ILogger logger, MuxState mode);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "IPC request to change MUX state. Target: {Mode}")]
    private static partial void LogMuxChangeRequest(ILogger logger, MuxState mode);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "[IMPORTANT] MUX switch setting successfully written to BIOS. A system RESTART is required for changes to take effect!")]
    private static partial void LogMuxChangeSuccess(ILogger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to write MUX setting to BIOS!")]
    private static partial void LogMuxChangeFailed(ILogger logger);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "ACPI: Supported MUX modes retrieved via {ApiType} API. (Mask: {Mask})")]
    private static partial void LogSupportedModesRead(ILogger logger, string apiType, byte mask);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "ACPI: Modern MUX query failed or not supported. Falling back to Legacy query...")]
    private static partial void LogLegacyFallbackAttempt(ILogger logger);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "ACPI: MUX switch is NOT supported on this hardware.")]
    private static partial void LogMuxNotSupported(ILogger logger);

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "ACPI: Blocked attempt to set unsupported MUX mode: {Mode}. Device capabilities mask prevents this.")]
    private static partial void LogUnsupportedMuxModeRequested(ILogger logger, MuxState mode);
    #endregion
}