using Nexus.Service.Hubs;
using Nexus.Service.Interfaces;

namespace Nexus.Service.Workers;

#pragma warning disable CA1812
internal sealed partial class NexusCoreWorker(
    ILogger<NexusCoreWorker> logger,
    ISystemCoordinator hardware,
    ITelemetryHub telemetryHub) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarting(logger);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1500));

        try
        {
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                bool isUiConnected = telemetryHub.HasActiveUiClients;

                var stats = await hardware.GetSystemStatsAsync(isUiConnected).ConfigureAwait(false);

                telemetryHub.Publish(stats);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogWorkerCriticalError(logger, ex);
            throw;
        }
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Nexus Core Worker is starting.")]
    private static partial void LogWorkerStarting(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Critical, Message = "A critical error occurred in the Nexus Core Worker main loop.")]
    private static partial void LogWorkerCriticalError(ILogger logger, Exception ex);
    #endregion
}