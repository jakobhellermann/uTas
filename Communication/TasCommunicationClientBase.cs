using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace uTas.Communication;

public abstract class TasCommunicationClientBase : CommunicationBase, IDisposable {
    private readonly int _port;
    private readonly int _retryInterval;

    protected TasCommunicationClientBase(int port, int retryInterval) {
        _port = port;
        _retryInterval = retryInterval;
    }


    #region IPC

    private TcpClient? _tcpClient;
    public bool Connected => _started;

    private CancellationTokenSource _retryLoopTokenSource = new();
    private CancellationToken RetryLoopCancellationToken => _retryLoopTokenSource.Token;

    private CancellationTokenSource _handlerTokenSource = new();
    private CancellationToken HandlerCancellationToken => _retryLoopTokenSource.Token;

    private bool _started;

    public void StartConnectionLoop() {
        Task.Run(RetryConnectionLoop);
    }

    private async Task RetryConnectionLoop() {
        while (true) {
            if (RetryLoopCancellationToken.IsCancellationRequested) return;

            try {
                await ConnectAsync(IPAddress.Loopback, _port);
                OnConnect();
                await base.Send(
                    _tcpClient!.GetStream(),
                    (byte)ClientOpCode.EstablishConnection,
                    new byte[] { },
                    RetryLoopCancellationToken
                );

                _started = true;
                await StartOnce();
            } catch (OperationCanceledException) {
                break;
            } catch (Exception e) {
                Log($"Could not connect, retrying in {_retryInterval}ms: {e.Message}");
                await Task.Delay(_retryInterval, RetryLoopCancellationToken);
            }


            _started = false;
        }


        Dispose();
    }

    public void Restart() {
        _handlerTokenSource.Cancel();
        _handlerTokenSource = new CancellationTokenSource();
        _tcpClient?.Dispose();
    }


    private async Task ConnectAsync(IPAddress address, int port) {
        _tcpClient?.Dispose();
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(address, port);
    }

    private async Task StartOnce() {
        if (_tcpClient is null || !_tcpClient.Connected)
            throw new Exception("attempted to Start unconnected client");

        try {
            while (_tcpClient.Connected && !HandlerCancellationToken.IsCancellationRequested) {
                var (opcode, data) = await Recv(_tcpClient.GetStream(), HandlerCancellationToken);
                try {
                    OnMessage((ServerOpCode)opcode, data);
                } catch (Exception e) {
                    Log($"Failed to handle message: {e}");
                }
            }
        } catch (EndOfStreamException) {
            Log($"Connection to Server lost");
        } catch (Exception e) {
            Log($"Error receiving from server: {e}");
        }
    }

    public async Task Send(ClientOpCode opcode, byte[] data) {
        if (_tcpClient is null) throw new Exception("attempted to send into unconnected client");

        try {
            await base.Send(_tcpClient.GetStream(), (byte)opcode, data, HandlerCancellationToken);
        } catch (Exception exception) {
            Log($"Exception talking to server, restarting: {exception.Message}");
            Restart();
        }
    }

    public async Task Send(ClientOpCode opcode, string message) {
        await Send(opcode, Encoding.UTF8.GetBytes(message));
    }

    public async Task Send(ClientOpCode opcode) {
        await Send(opcode, new byte[] { });
    }

    public void Dispose() {
        _handlerTokenSource.Cancel();
        _retryLoopTokenSource.Cancel();
        _tcpClient?.Dispose();
    }

    #endregion IPC

    public Task SendEstablishConnection() => Send(ClientOpCode.EstablishConnection);

    public Task SendCloseConnection() => Send(ClientOpCode.CloseConnection);

    public Task SendInfoText(string? infoText) => Send(ClientOpCode.SetInfoText, infoText ?? "");

    public Task SendStudioInfo(StudioInfo? studioInfo) =>
        Send(ClientOpCode.SetInfoText, (studioInfo ?? StudioInfo.Invalid).ToByteArray());

    private void OnMessage(ServerOpCode opcode, byte[] data) {
        switch (opcode) {
            case ServerOpCode.KeybindTriggered:
                var keybind = (TasKeybind)data[0];
                OnKeybindTriggered(keybind);
                break;
            case ServerOpCode.SendPath:
                var path = Encoding.UTF8.GetString(data);
                OnSendPath(path == "" ? null : path);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(opcode), opcode, null);
        }
    }


    protected abstract void Log(string msg);

    protected virtual void OnConnect() {
    }

    protected abstract void OnKeybindTriggered(TasKeybind keybind);

    protected abstract void OnSendPath(string? path);
}