using Microsoft.Extensions.DependencyInjection;
using Nexus.Client.Models;
using Nexus.Client.Serialization;
using System.Globalization;
using System.Text.Json;

namespace Nexus.Client.Extensions;

internal static class LocalizationExtensions
{
    private static readonly string[] SupportedCultures = ["en-US", "tr-TR"];
    private const string DefaultCulture = "en-US";

    private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "nexus-settings.json");

    public static IServiceCollection AddNexusLocalization(this IServiceCollection services)
    {
        string currentLanguage = DetermineAndSaveLanguage();

        var cultureInfo = new CultureInfo(currentLanguage);
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

        services.AddLocalization();

        return services;
    }

    private static string DetermineAndSaveLanguage()
    {
        NexusSettings? settings = null;

        if (File.Exists(SettingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                settings = JsonSerializer.Deserialize(json, NexusSettingsJsonContext.Default.NexusSettings);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (JsonException) { }
            catch (NotSupportedException) { }
        }

        bool needsSave = false;
        settings ??= new NexusSettings();

        string? lang = settings.Language;

        if (string.IsNullOrWhiteSpace(lang))
        {
            lang = CultureInfo.CurrentUICulture.Name;
            needsSave = true;
        }

        if (!SupportedCultures.Contains(lang))
        {
            if (lang.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
                lang = "tr-TR";
            else
                lang = DefaultCulture;

            needsSave = true;
        }

        settings.Language = lang;

        if (needsSave)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, NexusSettingsJsonContext.Default.NexusSettings);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (NotSupportedException) { }
        }

        return lang;
    }
}