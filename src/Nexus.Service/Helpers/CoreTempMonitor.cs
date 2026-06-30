using Nexus.Service.Models;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace Nexus.Service.Helpers;

internal sealed partial class CoreTempMonitor : IDisposable
{
    private readonly ILogger<CoreTempMonitor> _logger;
    private MemoryMappedFile? _sharedMemory;
    private MemoryMappedViewAccessor? _accessor;
    private bool _disposed;

    public CoreTempMonitor(ILogger<CoreTempMonitor> logger)
    {
        _logger = logger;
    }

    public async Task<bool> InitializeAsync(int maxRetries = 10, int delayMs = 500, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        int structSize = Unsafe.SizeOf<CoreTempSharedDataEx>();

        for (int i = 0; i < maxRetries; i++)
        {
            if (cancellationToken.IsCancellationRequested) return false;

            try
            {
                _sharedMemory = MemoryMappedFile.OpenExisting("CoreTempMappingObjectEx");
                _accessor = _sharedMemory.CreateViewAccessor(0, structSize, MemoryMappedFileAccess.Read);

                LogInitializationSuccess(_logger);
                return true;
            }
            catch (FileNotFoundException)
            {
                if (i == maxRetries - 1)
                {
                    LogSharedMemoryNotFound(_logger);
                    return false;
                }

                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogUnauthorizedAccessError(_logger, ex);
                return false;
            }
            catch (IOException ex)
            {
                LogIoError(_logger, ex);
                return false;
            }
            catch (PlatformNotSupportedException ex)
            {
                LogPlatformNotSupportedError(_logger, ex);
                return false;
            }
        }

        return false;
    }

    public unsafe int GetMaxCpuTemperature()
    {
        if (_disposed || _accessor == null || !_accessor.CanRead)
            return 0;

        try
        {
            _accessor.Read(0, out CoreTempSharedDataEx data);

            float maxTemp = 0;
            for (int i = 0; i < data.uiCoreCnt; i++)
            {
                if (data.fTemp[i] > maxTemp)
                {
                    maxTemp = data.fTemp[i];
                }
            }

            if (data.ucDeltaToTjMax == 1)
            {
                return (int)(data.uiTjMax[0] - maxTemp);
            }

            return (int)maxTemp;
        }
        catch (ObjectDisposedException ex)
        {
            LogAccessorDisposedError(_logger, ex);
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            LogInvalidOperationError(_logger, ex);
            return 0;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            LogArgumentOutOfRangeError(_logger, ex);
            return 0;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _accessor?.Dispose();
        _sharedMemory?.Dispose();

        _disposed = true;
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Core Temp shared memory initialized successfully.")]
    private static partial void LogInitializationSuccess(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Core Temp shared memory mapping object not found. Is Core Temp running and Shared Memory enabled?")]
    private static partial void LogSharedMemoryNotFound(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Insufficient permissions to access Core Temp shared memory.")]
    private static partial void LogUnauthorizedAccessError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "An I/O error occurred while accessing Core Temp shared memory.")]
    private static partial void LogIoError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Memory mapped files are not supported on this platform.")]
    private static partial void LogPlatformNotSupportedError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Attempted to read from a disposed memory mapped view accessor.")]
    private static partial void LogAccessorDisposedError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Invalid operation performed on the memory mapped view accessor.")]
    private static partial void LogInvalidOperationError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Attempted to read out of bounds from the memory mapped view accessor.")]
    private static partial void LogArgumentOutOfRangeError(ILogger logger, Exception ex);
    #endregion
}