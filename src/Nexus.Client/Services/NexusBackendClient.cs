using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Text.Json.Serialization.Metadata;
using Nexus.Shared.Interfaces;
using Nexus.Shared.Serialization;
using StreamJsonRpc;

namespace Nexus.Client.Services;

#pragma warning disable CA1812
internal sealed partial class NexusBackendClient : IAsyncDisposable
{
    private const string PipeName = "NexusIpcPipe";

    private NamedPipeClientStream? _pipeClient;
    private JsonRpc? _jsonRpc;
    public INexusRpcService? Proxy { get; private set; }

    public bool IsConnected => _pipeClient?.IsConnected ?? false;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ownership is transferred to JsonRpc which handles disposal.")]
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            await _pipeClient.ConnectAsync(3000, cancellationToken).ConfigureAwait(false);

            var formatter = new SystemTextJsonFormatter();
            
            formatter.JsonSerializerOptions.TypeInfoResolverChain.Add(NexusJsonContext.Default);
            formatter.JsonSerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());

            var messageHandler = new HeaderDelimitedMessageHandler(_pipeClient, _pipeClient, formatter);

            _jsonRpc = new JsonRpc(messageHandler);

            Proxy = _jsonRpc.Attach<INexusRpcService>();

            _jsonRpc.StartListening();

            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsonRpc != null)
            _jsonRpc.Dispose();
        if (_pipeClient != null)
            await _pipeClient.DisposeAsync().ConfigureAwait(false);
    }
}