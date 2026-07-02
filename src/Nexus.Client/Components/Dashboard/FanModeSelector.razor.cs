using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Nexus.Shared.Models;

namespace Nexus.Client.Components.Dashboard;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Blazor components must be public for the framework's routing and rendering mechanisms.")]
public sealed partial class FanModeSelector : ComponentBase
{
    [Parameter] public FanMode CurrentMode { get; set; } = FanMode.Auto;
    [Parameter] public EventCallback<FanMode> CurrentModeChanged { get; set; }
    [Parameter] public bool CoreTempExists { get; set; }

    private Task OnModeChanged(FanMode mode)
    {
        if (CurrentMode == mode)
            return Task.CompletedTask;

        return CurrentModeChanged.InvokeAsync(mode);
    }
}