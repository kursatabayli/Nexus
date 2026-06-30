using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Text.Json.Serialization.Metadata;
using Nexus.Shared.Interfaces;
using Nexus.Shared.Serialization;
using StreamJsonRpc;

namespace Nexus.Service.Workers;

#pragma warning disable CA1812
internal sealed partial class IpcServerWorker(
    ILogger<IpcServerWorker> logger,
    INexusRpcService rpcService) : BackgroundService
{
    private const string PipeName = "NexusIpcPipe";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServerStarting(logger, PipeName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pipeServer = new NamedPipeServerStream(
                    pipeName: PipeName,
                    direction: PipeDirection.InOut,
                    maxNumberOfServerInstances: NamedPipeServerStream.MaxAllowedServerInstances,
                    transmissionMode: PipeTransmissionMode.Byte,
                    options: PipeOptions.Asynchronous);

                LogWaitingForClient(logger);

                await pipeServer.WaitForConnectionAsync(stoppingToken).ConfigureAwait(false);
                LogClientConnected(logger);

                _ = HandleClientAsync(pipeServer, stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (IOException ex)
            {
                LogServerUnexpectedError(logger, ex);
                await Task.Delay(2000, stoppingToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogServerUnexpectedError(logger, ex);
                await Task.Delay(2000, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode")]
    private async Task HandleClientAsync(NamedPipeServerStream pipeServer, CancellationToken stoppingToken)
    {
        try
        {
            using var formatter = new SystemTextJsonFormatter();
            
            formatter.JsonSerializerOptions.TypeInfoResolverChain.Add(NexusJsonContext.Default);
            formatter.JsonSerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());

            using var messageHandler = new HeaderDelimitedMessageHandler(pipeServer, pipeServer, formatter);

            using var jsonRpc = new JsonRpc(messageHandler, rpcService);
            jsonRpc.StartListening();

            LogRpcChannelEstablished(logger);

            await jsonRpc.Completion.ConfigureAwait(false);

            LogClientDisconnected(logger);
        }
        catch (ConnectionLostException ex)
        {
            LogClientCommunicationIssue(logger, ex);
        }
        catch (IOException ex)
        {
            LogClientCommunicationIssue(logger, ex);
        }
        catch (RemoteInvocationException ex)
        {
            LogClientCommunicationIssue(logger, ex);
        }
        catch (Exception ex)
        {
            LogServerUnexpectedError(logger, ex);
            throw;
        }
        finally
        {
            await pipeServer.DisposeAsync().ConfigureAwait(false);
        }
    }

    #region Logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "IPC Server is starting. ({PipeName})")]
    private static partial void LogServerStarting(ILogger logger, string pipeName);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "IPC: Waiting for a new client connection...")]
    private static partial void LogWaitingForClient(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "IPC: A client has connected!")]
    private static partial void LogClientConnected(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "An unexpected error occurred in the IPC Server.")]
    private static partial void LogServerUnexpectedError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "IPC: JsonRpc channel successfully established and listening.")]
    private static partial void LogRpcChannelEstablished(ILogger logger);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "IPC: Client connection closed.")]
    private static partial void LogClientDisconnected(ILogger logger);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "IPC: A problem occurred during communication with the client.")]
    private static partial void LogClientCommunicationIssue(ILogger logger, Exception ex);
    #endregion
}