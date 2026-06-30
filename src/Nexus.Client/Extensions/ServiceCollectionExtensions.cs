using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Nexus.Client.Services;

namespace Nexus.Client.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddMudServices();

        services.AddNexusLocalization();

        services.AddSingleton<NexusBackendClient>();

        return services;
    }
}