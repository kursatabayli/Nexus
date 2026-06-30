namespace Nexus.Shared.Models;

public readonly record struct SystemStats(
    int CpuTemp,
    int GpuTemp,
    int CpuFanRpm,
    int GpuFanRpm,
    FanMode CurrentMode,
    MuxState CurrentMuxState
);