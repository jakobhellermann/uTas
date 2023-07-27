using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace uTas.Communication;

public abstract class TasCommunicationServerBase : IDisposable {
    private SemaphoreSlim _sendMutex = new(1);
    private SemaphoreSlim _recvMutex = new(1);

    private TcpListener? _listener;
    private List<TcpClient> _connectedClients = new();

    public async Task Start(IPAddress address, int port, CancellationToken cancellationToken) {
        _listener = new TcpListener(address, port);
        _listener.Start();
        Console.WriteLine($"Listening for connections on port {port}...");

        while (_listener != null) {
            var client = await _listener.AcceptTcpClientAsync(cancellationToken);
            Console.WriteLine($"Accepted client {client.Client.RemoteEndPoint}");

            _ = Task.Run(async () => await HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient cl) {
        using var client = cl;
        await using var stream = client.GetStream();

        _connectedClients.Add(client);

        var run = true;
        try {
            while (client.Connected && run) {
                var (opcode, data) = await Recv(stream);
                try {
                    run = run && !await ProcessRequest(opcode, data);
                } catch (Exception e) {
                    Console.WriteLine($"Failed to handle request: {e}");
                }
            }
        } catch (EndOfStreamException e) {
            Console.WriteLine($"Got unannounced EOF: {e}");
        } finally {
            _connectedClients.Remove(client);
        }

        OnClosedConnection(cl, !run);
    }

    protected async ValueTask SendToAll(byte opcode, byte[] data) {
        Console.WriteLine($"Sending to {_connectedClients.Count} clients");
        foreach (var client in _connectedClients)
            try {
                await Send(client.GetStream(), opcode, data);
            } catch (Exception e) {
                Console.WriteLine($"failed to send to a client, removing from list: {e}");
                _connectedClients.Remove(client);
            }
    }

    protected async ValueTask SendToAll(byte opcode, string data) {
        await SendToAll(opcode, Encoding.UTF8.GetBytes(data));
    }

    private async ValueTask Send(Stream stream, byte opcode, byte[] data) {
        await _sendMutex.WaitAsync();

        try {
            var header = new byte[5] { opcode, 0, 0, 0, 0 };
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan()[1..], (uint)data.Length);

            await stream.WriteAsync(header);
            await stream.WriteAsync(data);
        } finally {
            _sendMutex.Release();
        }
    }

    private async ValueTask<(byte, byte[])> Recv(Stream stream) {
        await _recvMutex.WaitAsync();

        try {
            var headerBuffer = new byte[5];
            await stream.ReadExactlyAsync(headerBuffer);
            var opcode = headerBuffer[0];

            var length = BinaryPrimitives.ReadUInt32BigEndian(headerBuffer.AsSpan()[1..]);

            var buffer = new byte[length];
            await stream.ReadExactlyAsync(buffer);


            return (opcode, buffer);
        } finally {
            _recvMutex.Release();
        }
    }


    /// <summary>
    ///     Handle the opcode
    /// </summary>
    /// <param name="opcodeByte"></param>
    /// <param name="request"></param>
    /// <returns>Whether the connection should be closed now</returns>
    protected abstract Task<bool> ProcessRequest(byte opcodeByte, byte[] request);

    protected abstract void OnClosedConnection(TcpClient client, bool gracefully);

    public void Dispose() {
        foreach (var client in _connectedClients) client.Dispose();

        _listener?.Stop();
        _listener = null;
    }
}