using Nexus.Service.Interfaces;
using Nexus.Service.Models;
using static Nexus.Service.Consts.HpWmiConstants;

namespace Nexus.Service.Services;

internal sealed class AcpiCmdService : IAcpiCmdService
{
    private readonly IAcpiService _acpiService;

    public AcpiCmdService(IAcpiService acpiService)
    {
        _acpiService = acpiService;
    }

    public Task<BiosCommandResult> GetFanTableAsync() =>
        _acpiService.ExecuteAsync(Operation.GameManager, Feature.VictusSGetFanTableQuery, [0, 0, 0, 0], BufferSize128);

    public Task<BiosCommandResult> SetThermalProfileSetupAsync() =>
        _acpiService.ExecuteAsync(Operation.GameManager, Feature.ThermalProfileSetup, [0, 0, 0, 0], BufferSize4);

    public Task<BiosCommandResult> SetMaxFanSpeedAsync(bool enable) =>
        _acpiService.ExecuteAsync(Operation.GameManager, Feature.FanSpeedMaxSetQuery, 
            [enable ? PayloadMaxFanEnable : PayloadMaxFanDisable, 0, 0, 0], BufferSize0);

    public Task<BiosCommandResult> SetFanSpeedAsync(byte cpuVal, byte gpuVal) =>
        _acpiService.ExecuteAsync(Operation.GameManager, Feature.VictusSFanSpeedSetQuery, [cpuVal, gpuVal, 0, 0], BufferSize0);

    public Task<BiosCommandResult> GetFanRpmsAsync() =>
        _acpiService.ExecuteAsync(Operation.GameManager, Feature.VictusSFanSpeedGetQuery, [0, 0, 0, 0], BufferSize128);

    public Task<BiosCommandResult> ReadMuxStateAsync() =>
        _acpiService.ExecuteAsync(Operation.Read, Feature.GraphicsMux, [0, 0, 0, 0], BufferSize4);

    public Task<BiosCommandResult> WriteMuxStateAsync(byte targetMode) =>
        _acpiService.ExecuteAsync(Operation.Write, Feature.GraphicsMux, [targetMode, 0, 0, 0], BufferSize4);

    public Task<BiosCommandResult> GetSystemDesignDataAsync() =>
        _acpiService.ExecuteAsync(Operation.GameManager, Feature.SystemDesignData, [0, 0, 0, 0], BufferSize128);
}