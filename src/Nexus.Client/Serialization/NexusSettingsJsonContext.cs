using Nexus.Client.Models;
using System.Text.Json.Serialization;

namespace Nexus.Client.Serialization;

[JsonSerializable(typeof(NexusSettings))]
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
internal sealed partial class NexusSettingsJsonContext : JsonSerializerContext
{
}