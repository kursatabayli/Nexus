using System;
using System.IO;
using System.Security;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexus.Service.Interfaces;
using Nexus.Shared.Models;
using Nexus.Shared.Serialization;

namespace Nexus.Service.Services;

#pragma warning disable CA1812
internal sealed partial class ConfigService : IConfigService
{
    private const string ConfigFileName = "config.json";
    private readonly ILogger<ConfigService> _logger;
    private readonly string _configFolder;
    private readonly string _configPath;

    public ConfigService(ILogger<ConfigService> logger)
    {
        _logger = logger;

        string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        _configFolder = Path.Combine(baseDir, "Nexus");
        _configPath = Path.Combine(_configFolder, ConfigFileName);
    }

    public FanConfig Load()
    {
        if (!File.Exists(_configPath))
            return EnsureConfigFileCreated();

        try
        {
            using var stream = new FileStream(_configPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var config = JsonSerializer.Deserialize(stream, NexusJsonContext.Default.FanConfig);

            if (config != null)
            {
                config.ValidateAndSortCurves();
                return config;
            }

            return EnsureConfigFileCreated();
        }
        catch (JsonException ex)
        {
            LogLoadError(ex);
            return EnsureConfigFileCreated();
        }
        catch (IOException ex)
        {
            LogLoadError(ex);
            return EnsureConfigFileCreated();
        }
        catch (UnauthorizedAccessException ex)
        {
            LogLoadError(ex);
            return EnsureConfigFileCreated();
        }
    }

    public void Save(FanConfig config)
    {
        try
        {
            if (!Directory.Exists(_configFolder))
                Directory.CreateDirectory(_configFolder);

            string tmpPath = _configPath + ".tmp";

            using (var stream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, config, NexusJsonContext.Default.FanConfig);
                stream.Flush();
            }

            File.Move(tmpPath, _configPath, overwrite: true);

            LogSaveSuccess();
        }
        catch (IOException ex)
        {
            LogSaveError(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogSaveError(ex);
        }
        catch (SecurityException ex)
        {
            LogSaveError(ex);
        }
    }

    private FanConfig EnsureConfigFileCreated()
    {
        try
        {
            if (!Directory.Exists(_configFolder))
                Directory.CreateDirectory(_configFolder);

            var defaultConfig = FanConfig.Default;
            Save(defaultConfig);
            return defaultConfig;
        }
        catch (IOException ex)
        {
            LogCriticalCreateError(ex);
            return FanConfig.Default;
        }
        catch (UnauthorizedAccessException ex)
        {
            LogCriticalCreateError(ex);
            return FanConfig.Default;
        }
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to load configuration file. Reverting to default settings.")]
    private partial void LogLoadError(Exception ex);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Configuration saved successfully.")]
    private partial void LogSaveSuccess();

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to save the configuration file.")]
    private partial void LogSaveError(Exception ex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Critical, Message = "Critical error: Could not create configuration directory or file.")]
    private partial void LogCriticalCreateError(Exception ex);
    #endregion
}