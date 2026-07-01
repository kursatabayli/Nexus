using Microsoft.Management.Infrastructure;

namespace Nexus.Service.Helpers;

internal static class HardwareIdentifier
{
    private static readonly Lazy<(string ProductId, string Manufacturer)> _baseBoardInfo = new(GetBaseBoardInfoInternal);
    private static readonly Lazy<string> _featureByte = new(GetFeatureByteInternal);

    public static string GetBaseBoardProductId() => _baseBoardInfo.Value.ProductId;
    public static string GetBaseBoardManufacturer() => _baseBoardInfo.Value.Manufacturer;
    public static string GetFeatureByte() => _featureByte.Value;

    private static (string ProductId, string Manufacturer) GetBaseBoardInfoInternal()
    {
        try
        {
            using var session = CimSession.Create(null);
            
            var instances = session.EnumerateInstances(@"root\cimv2", "Win32_BaseBoard");
            using var instance = instances.FirstOrDefault();
            
            if (instance != null)
            {
                string productId = instance.CimInstanceProperties["Product"]?.Value?.ToString() ?? "Unknown";
                string manufacturer = instance.CimInstanceProperties["Manufacturer"]?.Value?.ToString() ?? "Unknown";
                
                return (productId, manufacturer);
            }
        }
        catch (CimException) { }

        return ("Unknown", "Unknown");
    }

    private static string GetFeatureByteInternal()
    {
        try
        {
            using var session = CimSession.Create(null);

            var instances = session.QueryInstances(
                @"root\HP\InstrumentedBIOS", 
                "WQL", 
                "SELECT Value FROM HP_BIOSString WHERE Name = 'Feature Byte'");

            using var instance = instances.FirstOrDefault();
            return instance?.CimInstanceProperties["Value"]?.Value?.ToString() ?? string.Empty;
        }
        catch (CimException)
        {
            return string.Empty;
        }
    }
}