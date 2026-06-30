using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Nexus.Service.Models;

namespace Nexus.Service.Helpers;

internal static partial class WddmTelemetryProvider
{
    private const string Gdi32Dll = "gdi32.dll";
    private const int STATUS_SUCCESS = 0;

    [LibraryImport(Gdi32Dll)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static unsafe partial int D3DKMTQueryAdapterInfo(D3DKMT_QUERYADAPTERINFO* pData);

    [LibraryImport(Gdi32Dll)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static unsafe partial int D3DKMTEnumAdapters(byte* pEnumAdapters);

    public static unsafe uint InitializeNvidiaAdapterHandle(ILogger logger)
    {
        byte* enumBuffer = stackalloc byte[324];

        if (D3DKMTEnumAdapters(enumBuffer) != STATUS_SUCCESS)
        {
            LogEnumAdaptersFailed(logger);
            return 0;
        }

        uint numAdapters = *(uint*)enumBuffer;
        D3DKMT_ADAPTERINFO* adapters = (D3DKMT_ADAPTERINFO*)(enumBuffer + 4);

        for (int i = 0; i < numAdapters; i++)
        {
            uint hAdapter = adapters[i].hAdapter;
            if (hAdapter == 0) continue;

            D3DKMT_ADAPTERREGISTRYINFO registryInfo = default;
            D3DKMT_QUERYADAPTERINFO queryInfo = new()
            {
                hAdapter = hAdapter,
                Type = KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_ADAPTERREGISTRYINFO,
                pPrivateDriverData = &registryInfo,
                PrivateDriverDataSize = (uint)sizeof(D3DKMT_ADAPTERREGISTRYINFO)
            };

            if (D3DKMTQueryAdapterInfo(&queryInfo) == STATUS_SUCCESS)
            {
                string adapterName = new(registryInfo.AdapterString);

                if (adapterName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                {
                    LogNvidiaAdapterFound(logger, hAdapter, adapterName);
                    return hAdapter;
                }
            }
        }

        LogNvidiaAdapterNotFound(logger);
        return 0;
    }

    public static unsafe double GetGpuTemperature(uint hAdapter, ILogger logger)
    {
        if (hAdapter == 0) return 0;

        D3DKMT_ADAPTER_PERFDATA perfData = default;
        D3DKMT_QUERYADAPTERINFO queryInfo = new()
        {
            hAdapter = hAdapter,
            Type = KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_ADAPTERPERFDATA,
            pPrivateDriverData = &perfData,
            PrivateDriverDataSize = (uint)sizeof(D3DKMT_ADAPTER_PERFDATA)
        };

        if (D3DKMTQueryAdapterInfo(&queryInfo) == STATUS_SUCCESS)
        {
            return perfData.Temperature / 10.0;
        }

        LogQueryTemperatureFailed(logger, hAdapter);
        return 0;
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to enumerate WDDM adapters.")]
    private static partial void LogEnumAdaptersFailed(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "NVIDIA adapter found. Handle: {Handle}, Name: {AdapterName}")]
    private static partial void LogNvidiaAdapterFound(ILogger logger, uint handle, string adapterName);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "No NVIDIA adapter found during WDDM enumeration.")]
    private static partial void LogNvidiaAdapterNotFound(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Trace, Message = "Failed to query temperature for WDDM adapter handle {Handle}.")]
    private static partial void LogQueryTemperatureFailed(ILogger logger, uint handle);
    #endregion
}