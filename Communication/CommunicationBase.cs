using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace uTas.Communication;

public abstract class CommunicationBase {
    private SemaphoreSlim _sendMutex = new(1);
    private SemaphoreSlim _recvMutex = new(1);


    internal async Task Send(Stream stream, byte opcode, byte[] data, CancellationToken cancellationToken) {
        await _sendMutex.WaitAsync(cancellationToken);

        try {
            var lengthBytes = BitConverter.GetBytes((uint)data.Length);
            Array.Reverse(lengthBytes);
            var header = new[] { opcode, lengthBytes[0], lengthBytes[1], lengthBytes[2], lengthBytes[3] };
            await stream.WriteAsync(header, 0, header.Length, cancellationToken);
            await stream.WriteAsync(data, 0, data.Length, cancellationToken);
        } finally {
            _sendMutex.Release();
        }
    }

    internal async Task<(byte, byte[])> Recv(Stream stream, CancellationToken cancellationToken) {
        await _recvMutex.WaitAsync(cancellationToken);
        try {
            var headerBuffer = await stream.ReadExactlyAsync(5, cancellationToken);
            var opcode = headerBuffer[0];
            Array.Reverse(headerBuffer);
            var length = BitConverter.ToUInt32(headerBuffer, 0);
            var buffer = await stream.ReadExactlyAsync((int)length, cancellationToken);

            return (opcode, buffer);
        } finally {
            _recvMutex.Release();
        }
    }
}

internal static class StreamExtensions {
    internal static async Task<byte[]> ReadExactlyAsync(this Stream stream, int count,
        CancellationToken cancellationToken) {
        var buffer = new byte[count];
        var totalBytesRead = 0;

        while (totalBytesRead < count) {
            cancellationToken.ThrowIfCancellationRequested();
            var bytesRead = await stream.ReadAsync(buffer, totalBytesRead, count - totalBytesRead, cancellationToken);

            if (bytesRead == 0)
                throw new EndOfStreamException("End of stream reached before reading the required number of bytes.");

            totalBytesRead += bytesRead;
        }

        return buffer;
    }
}