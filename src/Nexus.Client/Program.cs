using InfiniFrame;
using InfiniFrame.BlazorWebView;
using Nexus.Client.Components;
using Nexus.Client.Extensions;

namespace Nexus.Client;

internal sealed class Program
{
    [STAThread]
    static void Main(string[] args)
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
        });

        InfiniFrameBlazorApp app = appBuilder.Build();

        app.Run();
    }
}