using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace uTas.Communication;

internal class ConnectedClient : CommunicationBase, IDisposable {
    private TcpClient _tcpClient;
    public bool Connected => _tcpClient.Connected;

    private CancellationToken _cancellationToken;

    public ConnectedClient(TcpClient tcpClient, CancellationToken cancellationToken) {
        _tcpClient = tcpClient;
        _cancellationToken = cancellationToken;
    }


    public async Task Send(byte opcode, byte[] data) =>
        await base.Send(_tcpClient.GetStream(), opcode, data, _cancellationToken);

    public async Task<(byte, byte[])> Recv() => await base.Recv(_tcpClient.GetStream(), _cancellationToken);

    public void Dispose() {
        _tcpClient.Dispose();
    }
}

public abstract class TasCommunicationServerBase : IDisposable {
    private TcpListener? _listener;

    private List<ConnectedClient> _connectedClients = new();

    private CancellationTokenSource _cancellationTokenSource = new();
    private CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public async Task Start(IPAddress address, int port) {
        _listener = new TcpListener(address, port);
        _listener.Start();
        Console.WriteLine($"Listening for connections on port {port}...");

        while (_listener != null) {
            var client = await _listener.AcceptTcpClientAsync();
            Console.WriteLine($"Accepted client {client.Client.RemoteEndPoint}");

            _ = Task.Run(async () => await HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient tcpClient) {
        using var client = new ConnectedClient(tcpClient, CancellationToken);
        _connectedClients.Add(client);

        var run = true;
        try {
            while (client.Connected && run && !CancellationToken.IsCancellationRequested) {
                var (opcode, data) = await client.Recv();
                try {
                    var shouldStop = await ProcessRequest((ClientOpCode)opcode, data);
                    run = run && !shouldStop;
                } catch (Exception e) {
                    Console.WriteLine($"Failed to handle request: {e}");
                }
            }
        } catch (EndOfStreamException e) {
            Console.WriteLine($"Got unannounced EOF: {e}");
        } finally {
            _connectedClients.Remove(client);
            tcpClient.Dispose();
        }

        OnClosedConnection(tcpClient, !run);
    }

    protected async Task SendToAll(byte opcode, byte[] data) {
        Console.WriteLine($"Sending to {_connectedClients.Count} clients");
        foreach (var client in _connectedClients)
            try {
                await client.Send(opcode, data);
            } catch (Exception e) {
                Console.WriteLine($"failed to send to a client, removing from list: {e}");
                client.Dispose();
                _connectedClients.Remove(client);
            }
    }

    protected async Task SendToAll(byte opcode, string data) {
        await SendToAll(opcode, Encoding.UTF8.GetBytes(data));
    }


    /// <summary>
    ///     Handle the opcode
    /// </summary>
    /// <param name="opcode"></param>
    /// <param name="request"></param>
    /// <returns>Whether the connection should be closed now</returns>
    protected abstract Task<bool> ProcessRequest(ClientOpCode opcode, byte[] request);

    protected abstract void OnClosedConnection(TcpClient client, bool gracefully);

    public void Dispose() {
        _listener?.Stop();
        _listener = null;
        _cancellationTokenSource.Cancel();

        foreach (var client in _connectedClients) client.Dispose();
    }
}