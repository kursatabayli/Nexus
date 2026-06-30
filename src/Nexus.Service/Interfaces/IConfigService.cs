using Nexus.Shared.Models;

namespace Nexus.Service.Interfaces;

internal interface IConfigService
{
    FanConfig Load();
    void Save(FanConfig config);
}