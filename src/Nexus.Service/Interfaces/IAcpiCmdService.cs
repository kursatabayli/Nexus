using Nexus.Service.Models;

namespace Nexus.Service.Interfaces;

internal interface IAcpiCmdService
{
    Task<BiosCommandResult> GetFanTableAsync();
    Task<BiosCommandResult> SetThermalProfileSetupAsync();
    Task<BiosCommandResult> SetMaxFanSpeedAsync(bool enable);
    Task<BiosCommandResult> SetFanSpeedAsync(byte cpuVal, byte gpuVal);
    Task<BiosCommandResult> GetFanRpmsAsync();
    
    Task<BiosCommandResult> ReadMuxStateAsync();
    Task<BiosCommandResult> WriteMuxStateAsync(byte targetMode);
    Task<BiosCommandResult> GetSystemDesignDataAsync();
}