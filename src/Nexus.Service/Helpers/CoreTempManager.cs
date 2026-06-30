using System.ComponentModel;
using System.Diagnostics;

namespace Nexus.Service.Helpers;

internal static partial class CoreTempManager
{
    private const string ProcessName = "Core Temp";
    private const string TargetSetting = "SnmpSharedMemory=";
    private const string TargetValue = "1;";
    private const string DefaultSettingLine = "SnmpSharedMemory=1;";

    public static void EnsureCoreTempReady(ILogger logger)
    {
        try
        {
            using var process = Process.GetProcessesByName(ProcessName).FirstOrDefault();

            if (process is not null)
            {
                string? exePath = process.MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    LogProcessPathNotFound(logger);
                    return;
                }

                string? directoryPath = Path.GetDirectoryName(exePath);
                if (string.IsNullOrEmpty(directoryPath))
                {
                    LogDirectoryPathNotFound(logger);
                    return;
                }

                string iniPath = Path.Combine(directoryPath, "CoreTemp.ini");

                if (!IsSharedMemoryEnabled(iniPath))
                {
                    LogSharedMemoryDisabled(logger);
                    process.Kill();
                    process.WaitForExit(3000);

                    EnableSharedMemory(iniPath);
                    StartCoreTemp(exePath, logger);
                }
                else
                {
                    LogSharedMemoryActive(logger);
                }
            }
            else
            {
                string? exePath = FindCoreTempInstallation();
                
                if (string.IsNullOrEmpty(exePath))
                {
                    LogCoreTempNotFound(logger);
                    return; 
                }

                string? directoryPath = Path.GetDirectoryName(exePath);
                if (string.IsNullOrEmpty(directoryPath))
                {
                    LogDirectoryPathNotFound(logger);
                    return;
                }

                string iniPath = Path.Combine(directoryPath, "CoreTemp.ini");

                if (!IsSharedMemoryEnabled(iniPath))
                {
                    LogEnablingSharedMemory(logger);
                    EnableSharedMemory(iniPath);
                }

                StartCoreTemp(exePath, logger);
            }
        }
        catch (Win32Exception ex)
        {
            LogWin32Error(logger, ex);
        }
        catch (IOException ex)
        {
            LogIoError(logger, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogUnauthorizedAccessError(logger, ex);
        }
        catch (InvalidOperationException ex)
        {
            LogInvalidOperationError(logger, ex);
        }
    }

    private static bool IsSharedMemoryEnabled(string iniPath)
    {
        if (!File.Exists(iniPath)) return false;

        foreach (var line in File.ReadLines(iniPath))
        {
            if (line.StartsWith(TargetSetting, StringComparison.OrdinalIgnoreCase))
            {
                return line.Contains(TargetValue, StringComparison.OrdinalIgnoreCase); 
            }
        }
        return false;
    }

    private static void EnableSharedMemory(string iniPath)
    {
        if (!File.Exists(iniPath)) return;

        var lines = File.ReadAllLines(iniPath).ToList();
        bool settingFound = false;
        int advancedSectionIndex = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim().Equals("[Advanced]", StringComparison.OrdinalIgnoreCase))
            {
                advancedSectionIndex = i;
            }
            else if (lines[i].StartsWith(TargetSetting, StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = DefaultSettingLine;
                settingFound = true;
                break;
            }
        }

        if (!settingFound && advancedSectionIndex != -1)
        {
            lines.Insert(advancedSectionIndex + 1, DefaultSettingLine);
        }

        File.WriteAllLines(iniPath, lines);
    }

    private static string? FindCoreTempInstallation()
    {
        string[] possiblePaths = [
            @"C:\Program Files\Core Temp\Core Temp.exe",
            @"C:\Program Files (x86)\Core Temp\Core Temp.exe"
        ];

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static void StartCoreTemp(string exePath, ILogger logger)
    {
        LogStartingCoreTemp(logger);
        
        ProcessStartInfo psi = new()
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath)!, 
            UseShellExecute = true,
            Verb = "runas",
        };
        
        using Process? process = Process.Start(psi);
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Core Temp process found, but the executable path could not be retrieved.")]
    private static partial void LogProcessPathNotFound(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Core Temp directory path could not be retrieved.")]
    private static partial void LogDirectoryPathNotFound(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Core Temp is running, but Shared Memory is disabled. Terminating process...")]
    private static partial void LogSharedMemoryDisabled(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Core Temp is running and Shared Memory is active. No intervention needed.")]
    private static partial void LogSharedMemoryActive(ILogger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Critical, Message = "CRITICAL: Core Temp installation could not be found on the system.")]
    private static partial void LogCoreTempNotFound(ILogger logger);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Enabling Shared Memory before starting Core Temp...")]
    private static partial void LogEnablingSharedMemory(ILogger logger);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Starting Core Temp process...")]
    private static partial void LogStartingCoreTemp(ILogger logger);

    [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "A Win32 error occurred while managing the Core Temp process.")]
    private static partial void LogWin32Error(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "An I/O error occurred while reading or writing the CoreTemp.ini file.")]
    private static partial void LogIoError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Insufficient permissions to modify the CoreTemp.ini file or process.")]
    private static partial void LogUnauthorizedAccessError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "The Core Temp process is in an invalid state.")]
    private static partial void LogInvalidOperationError(ILogger logger, Exception ex);
    #endregion
}