using Microsoft.AspNetCore.Components;
using MudBlazor;
using Nexus.Client.Resources;
using Nexus.Client.Services;
using Nexus.Shared.Models;
using System.Diagnostics.CodeAnalysis;

namespace Nexus.Client.Components.Dashboard;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Blazor components must be public for the framework's routing and rendering mechanisms.")]
public sealed partial class FanPanel : ComponentBase
{
    [Inject] ISnackbar Snackbar { get; set; } = default!;
    [Inject] NexusBackendClient BackendClient { get; set; } = default!;

    [Parameter] public SystemStats? CurrentStats { get; set; }
    [Parameter] public required IReadOnlyList<FanMode> SupportedModes { get; set; }

    public int CurrentCpuTemp => CurrentStats?.CpuTemp ?? 0;
    public int CurrentGpuTemp => CurrentStats?.GpuTemp ?? 0;

    private bool _isDirty;
    private FanConfig _draftConfig = FanConfig.Default;
    private FanConfig _activeConfig = FanConfig.Default;

    private IReadOnlyList<FanCurvePoint> _draftCpuCurve = [];
    private IReadOnlyList<FanCurvePoint> _draftGpuCurve = [];

    protected override async Task OnInitializedAsync()
    {
        SyncUIListsFromDraft();
        
        if (BackendClient.IsConnected && BackendClient.Proxy != null)
        {
            var serverConfig = await BackendClient.Proxy.GetFanConfigAsync().ConfigureAwait(true);
            
            if (serverConfig.CpuCurve.Count == 0 || serverConfig.GpuCurve.Count == 0)
            {
                _activeConfig = DeepClone(FanConfig.Default);
            }
            else
            {
                _activeConfig = serverConfig;
            }

            _draftConfig = DeepClone(_activeConfig);
            SyncUIListsFromDraft();
        }
        else
        {
            Snackbar.Add(Lang.FailedToFetchFanSettings, Severity.Error);
        }
    }

    private void SyncUIListsFromDraft()
    {
        _draftCpuCurve = [.. _draftConfig.CpuCurve];
        _draftGpuCurve = [.. _draftConfig.GpuCurve];
    }

    private void SyncDraftFromUILists()
    {
        _draftConfig.CpuCurve.Clear();
        foreach (var point in _draftCpuCurve) _draftConfig.CpuCurve.Add(point);

        _draftConfig.GpuCurve.Clear();
        foreach (var point in _draftGpuCurve) _draftConfig.GpuCurve.Add(point);
    }
    private void OnCurveEdited()
    {
        CheckIfDirty();
    }

    private async Task SaveChanges()
    {
        SyncDraftFromUILists();
        _draftConfig.ValidateAndSortCurves();
        _draftConfig.LastMode = CurrentStats?.CurrentMode ?? FanMode.Auto;

        if (BackendClient.IsConnected && BackendClient.Proxy != null)
        {
            await BackendClient.Proxy.ApplyFanConfigAsync(_draftConfig).ConfigureAwait(true);
            
            _activeConfig = DeepClone(_draftConfig);
            SyncUIListsFromDraft();
            CheckIfDirty();
        }
    }

    private void DiscardChanges()
    {
        _draftConfig = DeepClone(_activeConfig);
        SyncUIListsFromDraft();
        CheckIfDirty();
    }

    private void RestoreDefaults()
    {
        _draftConfig = DeepClone(FanConfig.Default);
        SyncUIListsFromDraft();
        CheckIfDirty();
    }

    private void CheckIfDirty()
    {
        SyncDraftFromUILists();
        bool isChanged = !AreConfigsEqual(_activeConfig, _draftConfig);
        if (_isDirty != isChanged)
        {
            _isDirty = isChanged;
        }
    }

    private static FanConfig DeepClone(FanConfig source)
    {
        var config = new FanConfig
        {
            LastMode = source.LastMode
        };

        config.CpuCurve.Clear();
        foreach (var point in source.CpuCurve)
            config.CpuCurve.Add(point);

        config.GpuCurve.Clear();
        foreach (var point in source.GpuCurve)
            config.GpuCurve.Add(point);

        return config;
    }

    private static bool AreConfigsEqual(FanConfig a, FanConfig b)
    {
        if (a.LastMode != b.LastMode) return false;
        if (!a.CpuCurve.SequenceEqual(b.CpuCurve)) return false;
        if (!a.GpuCurve.SequenceEqual(b.GpuCurve)) return false;
        return true;
    }

    private async Task HandleModeChange(FanMode newMode)
    {
        if (BackendClient.IsConnected && BackendClient.Proxy != null)
            await BackendClient.Proxy.SetFanModeAsync(newMode).ConfigureAwait(true);
    }

    private async Task HandleCpuAutoSave(IReadOnlyList<FanCurvePoint> newPoints)
    {
        _draftCpuCurve = newPoints;
        await SaveChanges().ConfigureAwait(true);
    }

    private async Task HandleGpuAutoSave(IReadOnlyList<FanCurvePoint> newPoints)
    {
        _draftGpuCurve = newPoints;
        await SaveChanges().ConfigureAwait(true);
    }
}