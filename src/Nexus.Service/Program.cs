using Nexus.Service.Controllers;
using Nexus.Service.Hubs;
using Nexus.Service.Interfaces;
using Nexus.Service.Services;
using Nexus.Service.Workers;
using Nexus.Shared.Interfaces;

namespace Nexus.Service;

internal sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "Nexus Background Service";
        });

        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.Services.AddSingleton<IConfigService, ConfigService>();
        builder.Services.AddSingleton<IAcpiCmdService, AcpiCmdService>();
        builder.Services.AddSingleton<IAcpiService, AcpiService>();
        builder.Services.AddSingleton<ISystemCoordinator, SystemCoordinator>();
        builder.Services.AddSingleton<IPlatformSupportService, PlatformSupportService>();
        builder.Services.AddSingleton<ITelemetryHub, TelemetryHub>();

        builder.Services.AddSingleton<IFanController, FanController>();
        builder.Services.AddSingleton<IMuxController, MuxController>();

        builder.Services.AddSingleton<INexusRpcService, NexusRpcService>();

        builder.Services.AddHostedService<IpcServerWorker>();
        builder.Services.AddHostedService<SystemInitializationWorker>();
        builder.Services.AddHostedService<NexusCoreWorker>();

        var host = builder.Build();

        host.Run();
    }
}