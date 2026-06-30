using Nexus.Shared.Models;
using System.Text.Json.Serialization;

namespace Nexus.Shared.Serialization;

[JsonSerializable(typeof(SystemStats))]
[JsonSerializable(typeof(FanConfig))]
[JsonSerializable(typeof(FanMode))]
[JsonSerializable(typeof(MuxState))]
[JsonSerializable(typeof(List<FanCurvePoint>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
public sealed partial class NexusJsonContext : JsonSerializerContext
{
}