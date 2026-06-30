using Nexus.Shared.Models;

namespace Nexus.Service.Interfaces;

internal interface IMuxController
{
    MuxState CurrentMuxState { get; }
    IReadOnlyList<MuxState> SupportedModes { get; }
    bool IsLegacyMux { get; }
    Task InitializeAsync();
    Task<bool> ChangeMuxMode(MuxState targetMode);
}