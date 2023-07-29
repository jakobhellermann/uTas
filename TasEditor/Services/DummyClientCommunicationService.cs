using System.Net;
using System.Threading.Tasks;
using uTas.Communication;

namespace TasEditor.Services;

public class DummyClientCommunicationService : IClientCommunicationService {
    public Task Start(IPAddress address, int port) => Task.CompletedTask;

    public void Shutdown() {
    }

    public Task SendKeybind(TasKeybind keybind) => Task.CompletedTask;

    public Task SendPath(string? path) => Task.CompletedTask;

    public void Dispose() {
    }
}