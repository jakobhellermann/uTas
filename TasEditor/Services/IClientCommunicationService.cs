using System;
using System.Net;
using System.Threading.Tasks;
using uTas.Communication;

namespace TasEditor.Services;

public interface IClientCommunicationService : IDisposable {
    public Task Start(IPAddress address, int port);

    public Task SendKeybind(TasKeybind keybind);

    public Task SendPath(string? path);
}