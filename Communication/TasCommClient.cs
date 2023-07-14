using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Communication;

public class Client : IAsyncDisposable, IDisposable {
    private readonly TcpClient _tcpClient;
    private NetworkStream _stream = null!;

    private Client(TcpClient client) {
        _tcpClient = client;
    }

    public static async ValueTask<Client> Connect(IPAddress address, int port) {
        var client = new Client(new TcpClient());
        await client._tcpClient.ConnectAsync(address, port);
        client._stream = client._tcpClient.GetStream();
        return client;
    }

    public async void Send(byte opcode, byte[] data) {
        var header = new byte[5] { opcode, 0, 0, 0, 0 };
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan()[1..], (uint)data.Length);
        await _stream.WriteAsync(header);
        await _stream.WriteAsync(data);
    }

    public async ValueTask<(byte, byte[])> Recv() {
        var headerBuffer = new byte[5];
        await _stream.ReadExactlyAsync(headerBuffer);
        var opcode = headerBuffer[0];
        var length = BinaryPrimitives.ReadUInt32BigEndian(headerBuffer.AsSpan()[1..]);
        var buffer = new byte[length];
        await _stream.ReadExactlyAsync(buffer);

        return (opcode, buffer);
    }

    public async ValueTask DisposeAsync() {
        await this._stream.DisposeAsync();
        this._tcpClient.Dispose();
    }

    public void Dispose() {
        _stream.Dispose();
        _tcpClient.Dispose();
    }
}