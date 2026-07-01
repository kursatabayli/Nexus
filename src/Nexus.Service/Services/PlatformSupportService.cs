using System.Text.Json;
using Nexus.Service.Helpers;
using Nexus.Service.Interfaces;
using Nexus.Shared.Models;
using Nexus.Shared.Serialization;

namespace Nexus.Service.Services;

#pragma warning disable CA1812
internal sealed partial class PlatformSupportService : IPlatformSupportService
{
    private readonly ILogger<PlatformSupportService> _logger;
    private readonly IAcpiCmdService _acpiCmdService;

    private static readonly string[] _cursedBoards = ["8607", "8746", "8747", "8748", "8749", "874A"];
    private readonly Dictionary<string, PerformancePlatformDto> _platformSupportMap = [];
    private readonly Dictionary<string, DevicePlatformDto> _devicePlatformMap = [];

    public bool IsManualFanSupported { get; private set; }
    public bool IsMaxFanSupported { get; private set; }

    public PlatformSupportService(
        ILogger<PlatformSupportService> logger,
        IAcpiCmdService acpiCmdService)
    {
        _logger = logger;
        _acpiCmdService = acpiCmdService;
    }

    public async Task<(bool IsManualFanSupported, bool IsMaxFanSupported)> InitializeAsync()
    {
        string boardId = HardwareIdentifier.GetBaseBoardProductId();

        var designDataResult = await _acpiCmdService.GetSystemDesignDataAsync().ConfigureAwait(false);
        byte[]? systemDesignData = designDataResult.Success ? designDataResult.ReturnData : null;

        IsManualFanSupported = DetermineManualFanSupport(systemDesignData);
        IsMaxFanSupported = await DetermineMaxFanSupportAsync(boardId, systemDesignData).ConfigureAwait(false);

        LogSupportStatus(_logger, IsManualFanSupported, IsMaxFanSupported);
        return (IsManualFanSupported, IsMaxFanSupported);
    }

    private static bool DetermineManualFanSupport(byte[]? data)
    {
        if (data != null && data.Length >= 5)
            return (data[4] & 1) > 0;

        return false;
    }

    private async Task<bool> DetermineMaxFanSupportAsync(string boardId, byte[]? data)
    {
        if (_cursedBoards.Contains(boardId)) return false;

        if (_platformSupportMap.Count == 0)
            await LoadPlatformSupportJsonAsync().ConfigureAwait(false);

        if (_platformSupportMap.TryGetValue(boardId, out PerformancePlatformDto? platformDto))
            if (platformDto.IsMaxFanSupported) return true;

        bool isBiosPerformanceControlSupport = DetermineBiosPerformanceControlSupport(data);

        if (isBiosPerformanceControlSupport) return true;

        if (_devicePlatformMap.Count == 0)
            await LoadDeviceListJsonAsync().ConfigureAwait(false);

        _devicePlatformMap.TryGetValue(boardId, out DevicePlatformDto? devicePlatform);
        bool isPavilionGaming = DetermineIfPavilionGaming(platformDto, devicePlatform);

        return isPavilionGaming;
    }

    private static bool DetermineBiosPerformanceControlSupport(byte[]? data)
    {
        if (data != null && data.Length >= 4)
            return data[3] == 1;

        return false;
    }

    private static bool DetermineIfPavilionGaming(PerformancePlatformDto? platformDto, DevicePlatformDto? devicePlatform)
    {
        if (platformDto != null && platformDto.IsLegacyV2)
            return true;

        if (devicePlatform != null)
        {
            if ((uint)(devicePlatform.Name - 59) <= 1u)
                return true;

            string manufacturer = HardwareIdentifier.GetBaseBoardManufacturer();
            string featureByte = HardwareIdentifier.GetFeatureByte();
            string displayName = devicePlatform.DisplayName ?? string.Empty;

            bool isHP = manufacturer.Contains("HP") || manufacturer.Contains("Hewlett-Packard");
            bool isOmen = displayName.Contains("OMEN", StringComparison.OrdinalIgnoreCase);
            bool isDuskersOrNone = devicePlatform.Name == DeviceType.Duskers || devicePlatform.Name == DeviceType.None;
            bool hasPavilionFlags = featureByte.Contains("7K") && featureByte.Contains("fd");

            if (isHP && !isOmen && !isDuskersOrNone && hasPavilionFlags)
                return true;
        }

        return false;
    }

    private async Task LoadPlatformSupportJsonAsync()
    {
        try
        {
            string jsonPath = Path.Combine(AppContext.BaseDirectory, "PerformancePlatformList.json");
            if (!File.Exists(jsonPath)) return;

            using FileStream stream = File.OpenRead(jsonPath);

            var platforms = await JsonSerializer.DeserializeAsync(stream, NexusJsonContext.Default.ListPerformancePlatformDto).ConfigureAwait(false);

            if (platforms != null)
            {
                foreach (var platform in platforms)
                {
                    if (!string.IsNullOrWhiteSpace(platform.SSID))
                        _platformSupportMap[platform.SSID] = platform;
                }
            }
        }
        catch (IOException ex)
        {
            LogJsonReadError(_logger, "PerformancePlatformList.json", ex);
        }
        catch (JsonException ex)
        {
            LogJsonReadError(_logger, "PerformancePlatformList.json", ex);
        }
    }

    private async Task LoadDeviceListJsonAsync()
    {
        try
        {
            string jsonPath = Path.Combine(AppContext.BaseDirectory, "DeviceList.json");
            if (!File.Exists(jsonPath)) return;

            using FileStream stream = File.OpenRead(jsonPath);

            var platforms = await JsonSerializer.DeserializeAsync(stream, NexusJsonContext.Default.ListDevicePlatformDto).ConfigureAwait(false);

            if (platforms != null)
            {
                foreach (var platform in platforms)
                {
                    if (platform.ProductNum == null) continue;
                    foreach (var product in platform.ProductNum)
                    {
                        if (!string.IsNullOrWhiteSpace(product))
                            _devicePlatformMap[product] = platform;
                    }
                }
            }
        }
        catch (IOException ex)
        {
            LogJsonReadError(_logger, "DeviceList.json", ex);
        }
        catch (JsonException ex)
        {
            LogJsonReadError(_logger, "DeviceList.json", ex);
        }
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Platform Support Discovered -> Manual Fan: {ManualSupport}, Max Fan: {MaxSupport}")]
    private static partial void LogSupportStatus(ILogger logger, bool manualSupport, bool maxSupport);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to read {FileName}")]
    private static partial void LogJsonReadError(ILogger logger, string fileName, Exception ex);
    #endregion
}