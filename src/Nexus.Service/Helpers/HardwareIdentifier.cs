using Microsoft.Management.Infrastructure;

namespace Nexus.Service.Helpers;

internal static class HardwareIdentifier
{
    public static string GetBaseBoardProductId()
    {
        try
        {
            using var session = CimSession.Create(null);
            var instances = session.EnumerateInstances(@"root\cimv2", "Win32_BaseBoard");
            
            using var instance = instances.FirstOrDefault();
            return instance?.CimInstanceProperties["Product"]?.Value?.ToString() ?? "Unknown";
        }
        catch (CimException)
        {
            return "Unknown";
        }
    }
}