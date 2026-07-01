namespace Nexus.Service.Interfaces;

internal interface IPlatformSupportService
{
    bool IsManualFanSupported { get; }
    bool IsMaxFanSupported { get; }
    Task<(bool IsManualFanSupported, bool IsMaxFanSupported)> InitializeAsync();
}