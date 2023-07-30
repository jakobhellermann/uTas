using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace uTas.Communication;

public abstract class TasCommunicationServerBase : IDisposable {
    #region IPC

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
                var (opcodeByte, data) = await client.Recv();
                var opcode = (ClientOpCode)opcodeByte;
                try {
                    ProcessRequest(opcode, data);
                    run = run && opcode is not ClientOpCode.CloseConnection;
                } catch (Exception e) {
                    Console.WriteLine($"Failed to handle request: {e}");
                }
            }
        } catch (EndOfStreamException e) {
            if (run) Console.WriteLine($"Got unannounced EOF: {e}");
        } finally {
            _connectedClients.Remove(client);
            tcpClient.Dispose();
            OnAnyConnectionClosed(!run);
        }
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

    #endregion

    private void ProcessRequest(ClientOpCode opcode, byte[] request) {
        switch (opcode) {
            case ClientOpCode.EstablishConnection:
                OnEstablishConnection();
                break;
            case ClientOpCode.CloseConnection:
                OnCloseConnection();
                break;
            case ClientOpCode.SetInfoText:
                var infoString = Encoding.UTF8.GetString(request);
                OnSetInfoText(infoString);
                break;
            case ClientOpCode.SetStudioInfo:
                var info = StudioInfo.FromByteArray(request);
                OnSetStudioInfo(info.CurrentLine == -1 ? null : info);
                break;
            case ClientOpCode.SendKeybindings:
                OnSendKeybindings();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(opcode), opcode, null);
        }
    }


    public async Task SendKeybind(TasKeybind keybind) {
        Console.WriteLine($"Sending keybind {keybind}");
        var data = new[] { (byte)keybind };
        await SendToAll((byte)ServerOpCode.KeybindTriggered, data);
    }

    public async Task SendPath(string? path) {
        Console.WriteLine($"Sending Path {path ?? "<null>"}");
        await SendToAll((byte)ServerOpCode.SendPath, path ?? "");
    }


    protected abstract void OnEstablishConnection();

    protected abstract void OnCloseConnection();

    protected abstract void OnSetInfoText(string infoText);

    protected abstract void OnSetStudioInfo(StudioInfo? info);

    protected abstract void OnSendKeybindings();

    protected abstract void OnAnyConnectionClosed(bool gracefully);

    public void Dispose() {
        _listener?.Stop();
        _listener = null;
        _cancellationTokenSource.Cancel();

        foreach (var client in _connectedClients) client.Dispose();
    }
}

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