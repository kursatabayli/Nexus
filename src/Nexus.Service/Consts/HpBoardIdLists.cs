namespace Nexus.Service.Consts;

internal static class HpBoardIdLists
{
    // hp-wmi.c -> victus_s_thermal_profile_boards listesinden alınmıştır
    public static readonly HashSet<string> VictusSBoards =
    [
        "8902", "8A44", "8A4D", "8BAB", "8B2F", "8BBE", "8BC2", "8BCA", 
        "8BCD", "8BD4", "8BD5", "8C76", "8C77", "8C78", "8C99", "8C9C", 
        "8D26", "8D41", "8D87", "8E35"
    ];

    // hp-wmi.c -> victus_thermal_profile_boards
    public static readonly HashSet<string> VictusBoards = ["88F8", "8A25"];

    // hp-wmi.c -> omen_thermal_profile_boards (Örnek olarak birkaçı eklendi, kernel dosyasından tümünü ekleyebilirsin)
    public static readonly HashSet<string> LegacyOmenBoards = 
    [
        "84DA", "84DB", "84DC", "8572", "8573", "8574", "8600", "8607"
    ];
}