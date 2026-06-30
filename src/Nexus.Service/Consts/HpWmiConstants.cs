namespace Nexus.Service.Consts;

internal static class HpWmiConstants
{
    internal static class Operation
    {
        public const int Read = 0x01;
        public const int Write = 0x02;
        public const int Odm = 0x03;
        public const int GameManager = 0x20008;
    }

    internal static class Feature
    {
        public const int ThermalProfileSetup = 0x10;
        public const int SystemDesignData = 0x28;
        public const int GraphicsMux = 0x52;
        
        public const int FanSpeedMaxSetQuery = 0x27;
        public const int VictusSFanSpeedGetQuery = 0x2D;
        public const int VictusSFanSpeedSetQuery = 0x2E;
        public const int VictusSGetFanTableQuery = 0x2F;
    }

    public const byte PayloadMaxFanDisable = 0x00;
    public const byte PayloadMaxFanEnable = 0x01;

    public const byte MuxModeUma = 1 << 0;
    public const byte MuxModeHybrid = 1 << 1;
    public const byte MuxModeDiscrete = 1 << 2;
    public const byte MuxModeOptimus = 1 << 3;

    public const byte MuxModeMask = 0x7F;
    public const byte MuxLegacyMask = MuxModeHybrid | MuxModeDiscrete;

    public const int BufferSize0 = 0;
    public const int BufferSize4 = 4;
    public const int BufferSize128 = 128;
    public const int BufferSize1024 = 1024;
    public const int BufferSize4096 = 4096;
}