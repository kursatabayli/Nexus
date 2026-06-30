using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Nexus.Client.Services;
using Nexus.Shared.Models;
using StreamJsonRpc;

namespace Nexus.Client.Components.Pages;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Blazor components must be public for the framework's routing and rendering mechanisms.")]
public sealed partial class Home : ComponentBase, IAsyncDisposable
{
    [Inject] NexusBackendClient BackendClient { get; set; } = null!;
    private bool _isConnected;
    private bool _connectionFailed;

    private SystemStats? _currentStats;
    
    private CancellationTokenSource? _streamCts;
    private IReadOnlyList<MuxState>? _supportedMuxModes;
    private bool _isMuxSupported => _supportedMuxModes != null && _supportedMuxModes.Count > 0;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await TryConnectAsync().ConfigureAwait(true);
            StateHasChanged(); 
        }
    }

    private async Task TryConnectAsync()
    {
        _connectionFailed = false;
        StateHasChanged();

        _isConnected = await BackendClient.ConnectAsync().ConfigureAwait(true);

        if (!_isConnected)
        {
            _connectionFailed = true;
        }
        else
        {
            if (BackendClient.Proxy != null)
                _supportedMuxModes = await BackendClient.Proxy.GetSupportedMuxModesAsync().ConfigureAwait(true);

            _ = StartTelemetryStreamAsync();
        }

        StateHasChanged();
    }

   private async Task StartTelemetryStreamAsync()
    {
        _streamCts = new CancellationTokenSource();
        
        try
        {
            if (BackendClient.IsConnected && BackendClient.Proxy != null)
            {
                await foreach (var stats in BackendClient.Proxy.StreamTelemetryAsync(_streamCts.Token).ConfigureAwait(true))
                {
                    _currentStats = stats;
                    await InvokeAsync(StateHasChanged).ConfigureAwait(true);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (ConnectionLostException) { SetDisconnectedState(); }
        catch (IOException) { SetDisconnectedState(); }
    }

    private void SetDisconnectedState()
    {
        _isConnected = false;
        _connectionFailed = true;
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task RetryConnection() => await TryConnectAsync().ConfigureAwait(true);
    
    public async ValueTask DisposeAsync()
    {
        if (_streamCts != null)
        {
            await _streamCts.CancelAsync().ConfigureAwait(false);
            _streamCts.Dispose();
        }
    }
}