using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using uTas.Communication;

namespace TasEditor.Services;

public interface IClientCommunicationService : IDisposable {
    public Task Start(IPAddress address, int port, CancellationToken cancellationToken);

    public Task SendKeybind(TasKeybind keybind);

    public Task SendPath(string? path);
}