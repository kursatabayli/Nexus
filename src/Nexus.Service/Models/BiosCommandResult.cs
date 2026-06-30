namespace Nexus.Service.Models;

internal sealed record BiosCommandResult(bool Success, int ReturnCode, byte[] ReturnData);