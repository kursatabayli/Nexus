using Microsoft.AspNetCore.Components;
using MudBlazor;
using Nexus.Client.Resources;
using Nexus.Client.Services;
using Nexus.Shared.Models;
using StreamJsonRpc;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Nexus.Client.Components.Dashboard;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Blazor components must be public for the framework's routing and rendering mechanisms.")]
public sealed partial class MuxPanel : ComponentBase
{
    [Inject] private IpcClient BackendClient { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public IReadOnlyList<MuxState> SupportedModes { get; set; } = [];
    [Parameter] public SystemStats? CurrentStats { get; set; }

    private MuxState? _targetMode;
    private MuxState _currentMuxState = MuxState.Undefined;

    protected override void OnParametersSet()
    {
        if (CurrentStats != null)
            _currentMuxState = CurrentStats.Value.CurrentMuxState;
    }

    private async Task HandleMuxChangeAsync(MuxState mode)
    {
        if (BackendClient.Proxy == null) return;

        _targetMode = mode;
        StateHasChanged();

        try
        {
            bool success = await BackendClient.Proxy.SetMuxStateAsync(mode).ConfigureAwait(true);

            if (success)
            {
                _currentMuxState = mode;

                var modeString = GetModeName(mode);
#pragma warning disable CA1863
                Snackbar.Add(string.Format(CultureInfo.CurrentCulture, Lang.MuxModeQueued, modeString), Severity.Success);
#pragma warning restore CA1863
            }
            else
            {
                Snackbar.Add(Lang.MuxChangeRejected, Severity.Error);
            }
        }
        catch (RemoteInvocationException)
        {
            Snackbar.Add(Lang.BackgroundServiceError, Severity.Error);
        }
        catch (ConnectionLostException)
        {
            Snackbar.Add(Lang.ConnectionLostError, Severity.Error);
        }
        catch (IOException)
        {
            Snackbar.Add(Lang.IoError, Severity.Error);
        }
        finally
        {
            _targetMode = null;
            StateHasChanged();
        }
    }

    private bool IsCurrentMode(MuxState mode) => _currentMuxState == mode;
    private bool IsAnyModeLoading() => _targetMode != null;

    private string GetCardClasses(MuxState mode)
    {
        string baseClasses = "mux-card d-flex flex-column w-100 rounded-xl";

        if (IsCurrentMode(mode))
            return $"{baseClasses} mud-border-primary border-solid border-2 shadow-lg mux-card-readonly";

        if (IsAnyModeLoading())
            return $"{baseClasses} mux-card-disabled";

        return $"{baseClasses} mux-card-clickable";
    }

    private Task HandleCardClickAsync(MuxState mode)
    {
        if (IsCurrentMode(mode) || IsAnyModeLoading())
            return Task.CompletedTask;

        return HandleMuxChangeAsync(mode);
    }

    private static string GetModeIcon(MuxState mode) => mode switch
    {
        MuxState.Hybrid => Icons.Material.Filled.AutoMode,
        MuxState.Discrete => Icons.Material.Filled.SportsEsports,
        MuxState.Optimus => Icons.Material.Filled.Memory,
        MuxState.UMA => Icons.Material.Filled.BatteryChargingFull,
        _ => Icons.Material.Filled.DeviceUnknown
    };

    private static string GetModeDescription(MuxState mode) => mode switch
    {
        MuxState.Hybrid => Lang.MuxDescHybrid,
        MuxState.Discrete => Lang.MuxDescDiscrete,
        MuxState.Optimus => Lang.MuxDescOptimus,
        MuxState.UMA => Lang.MuxDescUMA,
        _ => Lang.MuxDescUndefined
    };

    private static string GetModeName(MuxState mode) => mode switch
    {
        MuxState.Hybrid => Lang.MuxNameHybrid,
        MuxState.Discrete => Lang.MuxNameDiscrete,
        MuxState.Optimus => Lang.MuxNameOptimus,
        MuxState.UMA => Lang.MuxNameUMA,
        _ => mode.ToString()
    };
}