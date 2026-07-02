using System.Diagnostics;
using InfiniFrame;
using InfiniFrame.BlazorWebView;
using Nexus.Client.Components;
using Nexus.Client.Extensions;

namespace Nexus.Client;

internal sealed class Program
{
    private const string MutexName = @"Global\NexusClientMutex-{cbf2e120-032c-42bc-9658-6fe3ac1186d4}";
    private static Mutex? _appMutex;

    [STAThread]
    static void Main(string[] args)
    {
        _appMutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
            return;

        try
        {
            var appBuilder = InfiniFrameBlazorAppBuilder.CreateDefault(args);

            appBuilder.Services.AddApplicationServices();

            appBuilder.RootComponents.Add<App>("app");

            appBuilder.WithInfiniFrameWindowBuilder(builder =>
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "icons", "favicon.ico");
#if !DEBUG
                builder.SetDevToolsEnabled(true).SetContextMenuEnabled(true);
#endif
                builder.SetIconFile(iconPath)
                .SetTitle("Nexus")
                .SetUseOsDefaultSize(true)
                .SetResizable(true)
                .SetMaximized(true)
                .SetZoomEnabled(false)
                .Center();

                builder.RegisterWindowClosingHandler((window, e) =>
                {
                    // WORKAROUND: 
                    // The underlying UI framework currently hangs during its internal dispose/shutdown cycle, 
                    // causing the application to freeze as a zombie process.
                    // Since this application is a stateless thin client (handling data via IPC), 
                    // forcibly killing the process is completely safe and prevents the freeze.
                    // TODO: Remove this nuclear option and restore graceful shutdown once the framework's dispose bug is fixed.
                    Process.GetCurrentProcess().Kill();
                    return WindowClosingResult.Close;
                });
            });

            InfiniFrameBlazorApp app = appBuilder.Build();

            app.Run();
        }
        finally
        {
            _appMutex?.ReleaseMutex();
            _appMutex?.Dispose();
        }
    }
}