using System.Runtime.InteropServices;

namespace Nexus.Service.Models;

internal enum KMTQUERYADAPTERINFOTYPE : uint
{
    KMTQAITYPE_ADAPTERREGISTRYINFO = 8,
    KMTQAITYPE_ADAPTERPERFDATA = 62
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3DKMT_ADAPTERREGISTRYINFO
{
    public fixed char AdapterString[260];
    public fixed char BiosString[260];
    public fixed char DacType[260];
    public fixed char ChipType[260];
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3DKMT_ADAPTER_PERFDATA
{
    public uint PhysicalAdapterIndex;
    public ulong MemoryFrequency;
    public ulong MaxMemoryFrequency;
    public ulong MaxMemoryFrequencyOC;
    public ulong MemoryBandwidth;
    public ulong PCIEBandwidth;
    public uint FanRPM;
    public uint Power;
    public uint Temperature;
    public byte PowerStateOverride;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3DKMT_QUERYADAPTERINFO
{
    public uint hAdapter;
    public KMTQUERYADAPTERINFOTYPE Type;
    public void* pPrivateDriverData;
    public uint PrivateDriverDataSize;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3DKMT_ADAPTERINFO
{
    public uint hAdapter;
    public uint LuidLow;
    public int LuidHigh;
    public uint NumSources;
    public int bPrecisePresentRegionsPreferred;
}
