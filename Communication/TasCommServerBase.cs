using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Communication;

public abstract class TasCommServerBase {
    private SemaphoreSlim _sendMutex = new(1);
    private SemaphoreSlim _recvMutex = new(1);

    public async Task Start(IPAddress address, int port) {
        var listener = new TcpListener(address, port);
        listener.Start();
        Console.WriteLine($"Listening for connections on port {port}...");

        while (true) {
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine($"Accepted client {client.Client.RemoteEndPoint}");

            _ = Task.Run(async () => await HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient cl) {
        using var client = cl;
        await using var stream = client.GetStream();

        try {
            while (client.Connected) {
                var (opcode, buffer) = await Recv(stream);
                var res = ProcessRequest(opcode, buffer);
                if (res is var (responseOpcode, response)) {
                    await Send(stream, responseOpcode, response);
                }
            }
        } catch (EndOfStreamException) {
        }

        Console.WriteLine("Closed connection");
    }

    // TODO: hangs if the client doesn't dispose of the connection

    public async ValueTask Send(Stream stream, byte opcode, byte[] data) {
        await _sendMutex.WaitAsync();

        var header = new byte[5] { opcode, 0, 0, 0, 0 };
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan()[1..], (uint)data.Length);

        await stream.WriteAsync(header);
        await stream.WriteAsync(data);

        _sendMutex.Release();
    }

    public async ValueTask<(byte, byte[])> Recv(Stream stream) {
        await _recvMutex.WaitAsync();

        var headerBuffer = new byte[5];
        await stream.ReadExactlyAsync(headerBuffer);
        var opcode = headerBuffer[0];

        var length = BinaryPrimitives.ReadUInt32BigEndian(headerBuffer.AsSpan()[1..]);

        var buffer = new byte[length];
        await stream.ReadExactlyAsync(buffer);

        _recvMutex.Release();

        return (opcode, buffer);
    }


    protected abstract (byte, byte[])? ProcessRequest(byte opcodeByte, byte[] request);
}