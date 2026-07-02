using Nexus.Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexus.Shared.Serialization;

[JsonSerializable(typeof(PerformancePlatformDto))]
[JsonSerializable(typeof(List<PerformancePlatformDto>))]
[JsonSerializable(typeof(DevicePlatformDto))]
[JsonSerializable(typeof(List<DevicePlatformDto>))]
[JsonSerializable(typeof(LegacyV2SkuDto))]
[JsonSerializable(typeof(DeviceType))]
[JsonSerializable(typeof(SystemStats))]
[JsonSerializable(typeof(FanConfig))]
[JsonSerializable(typeof(FanMode))]
[JsonSerializable(typeof(MuxState))]
[JsonSerializable(typeof(List<FanCurvePoint>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSourceGenerationOptions(WriteIndented = false, UseStringEnumConverter = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
public sealed partial class NexusJsonContext : JsonSerializerContext
{
}