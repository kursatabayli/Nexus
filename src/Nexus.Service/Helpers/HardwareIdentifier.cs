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

    public static string GetBaseBoardManufacturer()
    {
        try
        {
            using var session = CimSession.Create(null);
            var instances = session.EnumerateInstances(@"root\cimv2", "Win32_BaseBoard");

            using var instance = instances.FirstOrDefault();
            return instance?.CimInstanceProperties["Manufacturer"]?.Value?.ToString() ?? "Unknown";
        }
        catch (CimException)
        {
            return "Unknown";
        }
    }

    public static string GetFeatureByte()
    {
        try
        {
            using var session = CimSession.Create(null);

            // HP'ye özel BIOS namespace'ini sorguluyoruz
            var instances = session.EnumerateInstances(@"root\HP\InstrumentedBIOS", "HP_BIOSString");

            foreach (var instance in instances)
            {
                var nameProperty = instance.CimInstanceProperties["Name"]?.Value?.ToString();

                // Name alanı "Feature Byte" olan kaydı buluyoruz
                if (string.Equals(nameProperty, "Feature Byte", StringComparison.OrdinalIgnoreCase))
                {
                    var value = instance.CimInstanceProperties["Value"]?.Value?.ToString();
                    instance.Dispose(); // Bulduğumuzda dispose etmeyi unutmayalım
                    return value ?? string.Empty;
                }

                instance.Dispose();
            }

            return string.Empty;
        }
        catch (CimException)
        {
            // Eğer cihaz HP değilse veya namespace yoksa (Örn: Masaüstü toplama PC) 
            // kod patlamasın, sessizce boş string dönsün. OGH'nin try-catch mantığıyla aynı.
            return string.Empty;
        }
    }
}