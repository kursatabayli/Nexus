using Nexus.Shared.Models;

namespace Nexus.Service.Interfaces;

internal interface IFanController : IDisposable
{
    FanMode CurrentMode { get; }
    Task InitializeAsync();
    Task SetFanModeAsync(FanMode mode);
    Task SetFanSpeedAsync(int cpuPercentage, int gpuPercentage);
    Task<(int CpuRpm, int GpuRpm)> ReadFanRpmsAsync();
}