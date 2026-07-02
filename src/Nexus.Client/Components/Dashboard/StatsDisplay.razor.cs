using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Nexus.Client.Models;
using Nexus.Client.Services;
using Nexus.Shared.Models;
using StreamJsonRpc;

namespace Nexus.Client.Components.Dashboard;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Blazor components must be public for the framework's routing and rendering mechanisms.")]
public sealed partial class StatsDisplay : ComponentBase
{
  [Parameter] public SystemStats? CurrentStats { get; set; }
  [Inject] IpcClient BackendClient { get; set; } = default!;
  [Inject] ISnackbar Snackbar { get; set; } = default!;
  private readonly SensorModel[] _sensors = new SensorModel[2];
  private static readonly string[] _colorCache = new string[101];

  private bool _isRetrying;

  static StatsDisplay()
  {
    for (int i = 0; i <= 100; i++)
      _colorCache[i] = GenerateHslColor(i);
  }

  protected override void OnInitialized()
  {
    _sensors[0] = new SensorModel { Label = "CPU", Icon = Icons.Material.Filled.Memory };
    _sensors[1] = new SensorModel { Label = "GPU", Icon = Icons.Material.Filled.VideogameAsset };
  }

  protected override void OnParametersSet()
  {
    if (CurrentStats is { } stats)
    {
      _sensors[0].Update(stats.CpuTemp, stats.CpuFanRpm, GetCachedColor(stats.CpuTemp));
      _sensors[1].Update(stats.GpuTemp, stats.GpuFanRpm, GetCachedColor(stats.GpuTemp));
    }
  }

  private async Task RetryCoreTempAsync()
  {
    if (BackendClient.Proxy == null) return;

    _isRetrying = true;
    StateHasChanged();

    try
    {
      bool success = await BackendClient.Proxy.RetryCoreTempConnectionAsync().ConfigureAwait(true);

      if (success)
      {
        Snackbar.Add("Core Temp bağlantısı başarıyla kuruldu!", Severity.Success);
      }
      else
      {
        Snackbar.Add("Core Temp arka planda bulunamadı. Yüklü olduğundan emin olun.", Severity.Error);
      }
    }
    catch (RemoteInvocationException ex)
    {
      Snackbar.Add($"RPC Error: {ex.Message}", Severity.Error);
    }
    catch (IOException ex)
    {
      Snackbar.Add($"IPC Error: {ex.Message}", Severity.Error);
    }
    finally
    {
      _isRetrying = false;
      StateHasChanged();
    }
  }

  private static string GetCachedColor(int temp)
  {
    if (temp < 0) return _colorCache[0];
    if (temp > 100) return _colorCache[100];
    return _colorCache[temp];
  }

  private static string GenerateHslColor(int temp)
  {
    double minTemp = 40.0;
    double maxTemp = 90.0;

    double clamped = Math.Clamp(temp, minTemp, maxTemp);
    double ratio = (clamped - minTemp) / (maxTemp - minTemp);

    double hue = 120 - (ratio * 120);

    return $"hsl({(int)hue}, 85%, 60%)";
  }
}