using Nexus.Service.Interfaces;

namespace Nexus.Service.Workers;

#pragma warning disable CA1812
internal sealed class SystemInitializationWorker(ISystemCoordinator hardwareService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) 
        => await hardwareService.InitializeAsync(cancellationToken).ConfigureAwait(false);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}