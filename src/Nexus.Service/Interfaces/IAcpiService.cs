using Nexus.Service.Models;

namespace Nexus.Service.Interfaces;

internal interface IAcpiService
{
    Task<BiosCommandResult> ExecuteAsync(int command, int commandType, byte[] inputData, int returnDataSize);
}