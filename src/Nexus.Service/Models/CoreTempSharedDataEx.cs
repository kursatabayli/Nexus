using System.Runtime.InteropServices;

namespace Nexus.Service.Models;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal unsafe struct CoreTempSharedDataEx
{
    public fixed uint uiLoad[256];
    public fixed uint uiTjMax[128];
    public uint uiCoreCnt;
    public uint uiCPUCnt;
    public fixed float fTemp[256];
    public float fVID;
    public float fCPUSpeed;
    public float fFSBSpeed;
    public float fMultiplier;
    public fixed byte sCPUName[100];
    public byte ucFahrenheit;
    public byte ucDeltaToTjMax;
    public byte ucTdpSupported;
    public byte ucPowerSupported;
    public uint uiStructVersion;
    public fixed uint uiTdp[128];
    public fixed float fPower[128];
    public fixed float fMultipliers[256];
}