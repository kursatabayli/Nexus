using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Nexus.Client.Resources;
using Nexus.Shared.Models;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Nexus.Client.Components.Shared;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Blazor components must be public for the framework's routing and rendering mechanisms.")]
public sealed partial class CurveEditor : ComponentBase, IAsyncDisposable
{
    [Parameter] public IReadOnlyList<FanCurvePoint> Points { get; set; } = [];
    [Parameter] public string Title { get; set; } = "Fan Curve";
    [Parameter] public string Color { get; set; } = "#00e5ff";
    [Parameter] public int CurrentTemp { get; set; } = 0;
    [Parameter] public EventCallback<IReadOnlyList<FanCurvePoint>> PointsChanged { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<FanCurvePoint>> OnAutoSaveRequested { get; set; } // BUNU EKLE
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private List<FanCurvePoint> _localPoints = [];

    private ElementReference _containerRef;
    private DotNetObjectReference<CurveEditor>? _dotNetRef;

    private readonly string _guid = Guid.NewGuid().ToString("N");

    private double _width = 600;
    private const double Height = 250;
    private const double PaddingX = 20;

    private int? _draggingIndex;
    private int? _hoverIndex;

    private string _cachedLinePath = "";
    private string _cachedAreaPath = "";

    private string LinePath => _cachedLinePath;
    private string AreaPath => _cachedAreaPath;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);

            await JS.InvokeVoidAsync("curveEditor.addResizeListener", _containerRef, _dotNetRef).ConfigureAwait(true);

            double initialWidth = await JS.InvokeAsync<double>("curveEditor.getElementWidth", _containerRef).ConfigureAwait(true);
            if (initialWidth > 0)
            {
                _width = initialWidth;
                UpdatePathCaches();
                StateHasChanged();
            }
        }
    }

    protected override void OnParametersSet()
    {
        if (!_draggingIndex.HasValue)
        {
            if (!ArePointsEqual(_localPoints, Points))
            {
                _localPoints = [.. Points];
                UpdatePathCaches();
            }
        }
    }

    private static bool ArePointsEqual(List<FanCurvePoint> a, IReadOnlyList<FanCurvePoint> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].Temperature != b[i].Temperature || a[i].Speed != b[i].Speed)
                return false;
        }
        return true;
    }

    private void UpdatePathCaches()
    {
        if (_localPoints.Count == 0) return;
        _cachedLinePath = BuildPath(false);
        _cachedAreaPath = BuildPath(true);
    }

    [JSInvokable]
    public void OnResize(double width)
    {
        if (_width != width)
        {
            _width = width;
            UpdatePathCaches();
            StateHasChanged();
        }
    }

    private void HandlePointerDown(PointerEventArgs e)
    {
        int? closestIndex = null;
        double minDistance = 20.0;

        for (int i = 0; i < _localPoints.Count; i++)
        {
            double dist = Math.Abs(e.OffsetX - GetPointX(i));
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        if (closestIndex.HasValue)
        {
            _draggingIndex = closestIndex.Value;
            UpdateSpeed(_draggingIndex.Value, e.OffsetY);
        }
    }

    private async Task HandlePointerUp(PointerEventArgs e)
    {
        if (_draggingIndex.HasValue)
        {
            _draggingIndex = null;

            await PointsChanged.InvokeAsync(_localPoints).ConfigureAwait(true);
        }
    }

    private void HandlePointerMove(PointerEventArgs e)
    {
        if (_draggingIndex.HasValue)
        {
            UpdateSpeed(_draggingIndex.Value, e.OffsetY);
        }
        else
        {
            int? newHoverIndex = null;
            double minDistance = 20.0;

            for (int i = 0; i < _localPoints.Count; i++)
            {
                double dist = Math.Abs(e.OffsetX - GetPointX(i));
                if (dist < minDistance)
                {
                    minDistance = dist;
                    newHoverIndex = i;
                }
            }

            if (_hoverIndex != newHoverIndex)
            {
                _hoverIndex = newHoverIndex;
            }
        }
    }
    private async Task HandlePointerLeave(PointerEventArgs e)
    {
        _hoverIndex = null;
        await HandlePointerUp(e).ConfigureAwait(true);
    }

    private void UpdateSpeed(int index, double currentY)
    {
        int newSpeed = YToSpeed(currentY);

        if (_localPoints[index].Speed != newSpeed)
        {
            _localPoints[index] = _localPoints[index] with { Speed = newSpeed };
            UpdatePathCaches();
        }
    }

    private double GetCurrentTempX()
    {
        if (_localPoints.Count == 0) return -100;

        double minTemp = _localPoints[0].Temperature;
        double maxTemp = _localPoints[^1].Temperature;

        double clamped = Math.Clamp(CurrentTemp, minTemp, maxTemp);
        double range = maxTemp - minTemp;
        double normalized = range > 0 ? (clamped - minTemp) / range : 0;

        return PaddingX + (normalized * (_width - 2 * PaddingX));
    }

    private double GetPointX(int index)
    {
        if (_localPoints.Count == 0) return PaddingX;

        double minTemp = _localPoints[0].Temperature;
        double maxTemp = _localPoints[^1].Temperature;
        double range = maxTemp - minTemp;

        double normalized = range > 0 ? (_localPoints[index].Temperature - minTemp) / range : 0;

        return PaddingX + (normalized * (_width - 2 * PaddingX));
    }

    private static double GetSpeedY(int speed) => Height - speed / 100.0 * Height;

    private static int YToSpeed(double y) => Math.Clamp((int)Math.Round((Height - y) / Height * 100.0), 0, 100);

    private static string Invariant(double value) => value.ToString(CultureInfo.InvariantCulture);

    private string BuildPath(bool isArea)
    {
        if (_localPoints.Count == 0) return "";

        var sb = new StringBuilder(_localPoints.Count * 25 + 50);

        for (int i = 0; i < _localPoints.Count; i++)
        {
            sb.Append(i == 0 ? "M" : "L");

            sb.Append(' ');
            sb.Append(Invariant(GetPointX(i)));
            sb.Append(' ');
            sb.Append(Invariant(GetSpeedY(_localPoints[i].Speed)));
        }

        if (isArea)
        {
            sb.Append(" L ");
            sb.Append(Invariant(GetPointX(_localPoints.Count - 1)));
            sb.Append(' ');
            sb.Append(Invariant(Height));

            sb.Append(" L ");
            sb.Append(Invariant(GetPointX(0)));
            sb.Append(' ');
            sb.Append(Invariant(Height));
            sb.Append(" Z");
        }

        return sb.ToString();
    }

    private async Task OpenAdvancedSettingsDialog()
    {
        var parameters = new DialogParameters<PointEditorDialog> { { x => x.Points, _localPoints } };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };

#pragma warning disable CA1863
        var dialog = await DialogService.ShowAsync<PointEditorDialog>(string.Format(CultureInfo.CurrentCulture, Lang.PointSettingsTitleFormat, Title), parameters, options).ConfigureAwait(true);
#pragma warning restore CA1863
        var result = await dialog.Result.ConfigureAwait(true);

        if (result is not null && !result.Canceled && result.Data is List<FanCurvePoint> updatedPoints)
        {
            _localPoints = updatedPoints;

            UpdatePathCaches();

            await PointsChanged.InvokeAsync(_localPoints).ConfigureAwait(true);

            if (OnAutoSaveRequested.HasDelegate)
                await OnAutoSaveRequested.InvokeAsync(_localPoints).ConfigureAwait(true);

            await PointsChanged.InvokeAsync(_localPoints).ConfigureAwait(true);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        _dotNetRef = null;

        try
        {
            await JS.InvokeVoidAsync("curveEditor.removeResizeListener", _containerRef).ConfigureAwait(true);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
    }
}