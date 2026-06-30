using Microsoft.AspNetCore.Components;
using MudBlazor;
using Nexus.Client.Resources;
using Nexus.Shared.Helpers;
using Nexus.Shared.Models;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Nexus.Client.Components.Shared;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Blazor components must be public for the framework's routing and rendering mechanisms.")]
public sealed partial class PointEditorDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] ISnackbar Snackbar { get; set; } = default!;
    [Parameter] public IReadOnlyList<FanCurvePoint> Points { get; set; } = [];

    private List<FanCurvePoint> _editPoints = [];
    private int? _newTemp;

    protected override void OnInitialized() => _editPoints = [.. Points];

    private void AddNewPoint()
    {
        if (!_newTemp.HasValue) return;
        int temp = _newTemp.Value;

        if (temp <= 0 || temp >= 100)
        {
            Snackbar.Add(Lang.TempBoundsWarning, Severity.Warning);
            return;
        }

        if (_editPoints.Any(p => p.Temperature == temp))
        {
#pragma warning disable CA1863
            Snackbar.Add(string.Format(CultureInfo.CurrentCulture, Lang.PointExistsWarning, temp), Severity.Warning);
#pragma warning restore CA1863
            return;
        }

        int autoSpeed = FanCurveCalculator.CalculatePercentage(temp, _editPoints.OrderBy(p => p.Temperature).ToArray());

        _editPoints.Add(new FanCurvePoint(temp, autoSpeed));
        _editPoints = [.. _editPoints.OrderBy(p => p.Temperature)];

        _newTemp = null;
    }

    private void RemovePoint(FanCurvePoint point)
    {
        if (point.Temperature == 0 || point.Temperature == 100) return;
        _editPoints.Remove(point);
    }

    private void Submit() => MudDialog.Close(DialogResult.Ok(_editPoints));

    private void Cancel() => MudDialog.Cancel();
}