using System.Text.Json;

namespace Nexus.Shared.Models;

public sealed class PerformancePlatformDto
{
    public string SSID { get; set; } = string.Empty;
    public bool IsMaxFanSupported { get; set; }
    public LegacyV2SkuDto? GetLegacyV2Sku { get; set; }
    public bool IsLegacyV2 => GetLegacyV2Sku?.Modes?.Count > 0;
}

public sealed class LegacyV2SkuDto
{
    public IReadOnlyList<JsonElement>? Modes { get; set; }
}

public sealed class DevicePlatformDto
{
    public DeviceType Name { get; set; }
    public string? DisplayName { get; set; }
    public IReadOnlyList<string>? ProductNum { get; set; }
}

public enum DeviceType
{
    None,
    Dragons10,
    Hurricane10,
    Pirates10,
    Pirates11,
    Marlins10,
    Marlins11,
    Gamora10,
    Gamora11,
    Perseus10,
    Typhon10,
    Typhon12,
    Tracer10,
    DRX,
    Milos10,
    Santorini10,
    HolmesG,
    TracerDFEdorasDF,
    TracerDFMoria2,
    Orisa10,
    Orisa11,
    OrisaA,
    OrisaA11,
    Cyprus,
    Mallorca,
    Nimbatus,
    Duskers,
    Starmade,
    Valkyrie,
    Modena,
    HPPC,
    Ralph,
    Cybug,
    Taffyta,
    Calhoun,
    Vanellope,
    HPPCPavOPP,
    Articuno,
    ArticunoA,
    Lapras,
    LaprasA,
    Noctali,
    NoctaliA,
    Hendricks,
    Nolet,
    Opihr,
    Roku,
    Brunobear,
    Roaree,
    Thetiger,
    Bigred,
    Quaker,
    Edna,
    Voco,
    Avid,
    Ritchie,
    Snowball,
    SnowballA,
    Dojo,
    Snowflake,
    SnowflakeA,
    Hanna,
    Vibrance,
    Khalilah,
    ArticiceA,
    Pocari,
    Propel,
    Coldsnap,
    Frostbyte,
    Glacia,
    Other
}