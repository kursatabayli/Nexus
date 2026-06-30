using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Nexus.Service.Extensions;
using Nexus.Service.Interfaces;
using Nexus.Service.Models;
using static Nexus.Service.Consts.HpWmiConstants;

namespace Nexus.Service.Services;

#pragma warning disable CA1812
internal sealed partial class AcpiService : IAcpiService, IDisposable
{
    private static class WmiConfig
    {
        public const string Namespace = @"root\wmi";
        public const string ClassName = "hpqBIntM";
        public const string InDataClassName = "hpqBDataIn";

        public const string Method0 = "hpqBIOSInt0";
        public const string Method4 = "hpqBIOSInt4";
        public const string Method128 = "hpqBIOSInt128";
        public const string Method1024 = "hpqBIOSInt1024";
        public const string Method4096 = "hpqBIOSInt4096";

        public const string PropSign = "Sign";
        public const string PropCommand = "Command";
        public const string PropCommandType = "CommandType";
        public const string PropSize = "Size";
        public const string PropDataIn = "hpqBData";

        public const string ParamInData = "InData";
        public const string ParamOutData = "OutData";

        public const string PropReturnCode = "rwReturnCode";
        public const string PropDataOut = "Data";

        public static readonly byte[] Signature = "SECU"u8.ToArray();
        public const uint DefaultFailCode = 255u;
    }

    private readonly CimSession _session;
    private readonly ILogger<AcpiService> _logger;
    private bool _disposed;

    public AcpiService(ILogger<AcpiService> logger)
    {
        _logger = logger;

        using var options = new DComSessionOptions();
        _session = CimSession.Create(null, options);

        LogSessionCreated(_logger);
    }

    public async Task<BiosCommandResult> ExecuteAsync(int command, int commandType, byte[]? inputData, int returnDataSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            var instances = _session.EnumerateInstancesAsync(WmiConfig.Namespace, WmiConfig.ClassName);

            using CimInstance? targetInstance = await instances.FirstOrDefaultAsync().ConfigureAwait(false);

            if (targetInstance == null)
            {
                LogWmiDeviceNotFound(_logger);
                return new BiosCommandResult(false, -1, []);
            }

            string methodName = returnDataSize switch
            {
                > BufferSize1024 => WmiConfig.Method4096,
                > BufferSize128 => WmiConfig.Method1024,
                > BufferSize4 => WmiConfig.Method128,
                > BufferSize0 => WmiConfig.Method4,
                _ => WmiConfig.Method0
            };

            int inputDataSize = inputData?.Length ?? 0;

            uint normalizedSize = inputDataSize switch
            {
                > BufferSize1024 => BufferSize4096,
                > BufferSize128 => BufferSize1024,
                > BufferSize4 => BufferSize128,
                > BufferSize0 => BufferSize4,
                _ => BufferSize0
            };

            byte[] paddedData = new byte[normalizedSize == BufferSize0 ? 1 : normalizedSize];

            if (inputData != null && inputDataSize > 0)
                Array.Copy(inputData, paddedData, Math.Min(inputDataSize, paddedData.Length));

            using CimInstance inDataInstance = new(WmiConfig.InDataClassName, WmiConfig.Namespace);

            inDataInstance.CimInstanceProperties.Add(CimProperty.Create(WmiConfig.PropSign, WmiConfig.Signature, CimType.UInt8Array, CimFlags.None));
            inDataInstance.CimInstanceProperties.Add(CimProperty.Create(WmiConfig.PropCommand, (uint)command, CimType.UInt32, CimFlags.None));
            inDataInstance.CimInstanceProperties.Add(CimProperty.Create(WmiConfig.PropCommandType, (uint)commandType, CimType.UInt32, CimFlags.None));
            inDataInstance.CimInstanceProperties.Add(CimProperty.Create(WmiConfig.PropSize, normalizedSize, CimType.UInt32, CimFlags.None));
            inDataInstance.CimInstanceProperties.Add(CimProperty.Create(WmiConfig.PropDataIn, paddedData, CimType.UInt8Array, CimFlags.None));

            using CimMethodParametersCollection methodParams =
            [
                CimMethodParameter.Create(WmiConfig.ParamInData, inDataInstance, CimType.Instance, CimFlags.In)
            ];

            using CimMethodResult result = _session.InvokeMethod(targetInstance, methodName, methodParams);

            if (result.OutParameters[WmiConfig.ParamOutData]?.Value is CimInstance outDataInstance)
            {
                using (outDataInstance)
                {
                    uint returnCode = (uint)(outDataInstance.CimInstanceProperties[WmiConfig.PropReturnCode]?.Value ?? 255u);
                    byte[]? returnedData = outDataInstance.CimInstanceProperties[WmiConfig.PropDataOut]?.Value as byte[];

                    return new BiosCommandResult(true, (int)returnCode, returnedData ?? []);
                }
            }
        }
        catch (CimException ex)
        {
            if (ex.NativeErrorCode == NativeErrorCode.NotFound)
            {
                return new BiosCommandResult(true, 0, []);
            }
            LogMmiError(_logger, ex.Message, (int)ex.NativeErrorCode, ex);
        }
        catch (ArgumentException ex)
        {
            LogUnexpectedError(_logger, ex);
        }
        catch (InvalidOperationException ex)
        {
            LogUnexpectedError(_logger, ex);
        }

        return new BiosCommandResult(false, -1, []);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _session?.Dispose();
        LogSessionDisposed(_logger);

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "[ACPI] CimSession created successfully.")]
    private static partial void LogSessionCreated(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "[ACPI] Target WMI device not found!")]
    private static partial void LogWmiDeviceNotFound(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "[ACPI] MMI Error: {Message} (Code: {ErrorCode})")]
    private static partial void LogMmiError(ILogger logger, string message, int errorCode, Exception ex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "[ACPI] An unexpected error occurred during execution.")]
    private static partial void LogUnexpectedError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "[ACPI] CimSession cleaned up successfully.")]
    private static partial void LogSessionDisposed(ILogger logger);
    #endregion
}