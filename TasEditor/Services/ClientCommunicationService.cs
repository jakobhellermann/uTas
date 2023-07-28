using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TasEditor.ViewModels;
using uTas.Communication;

namespace TasEditor.Services;

public class ClientCommunicationService : TasCommunicationServerBase, IClientCommunicationService {
    private readonly MainViewModel _viewModel;

    public ClientCommunicationService(MainViewModel viewModel) {
        _viewModel = viewModel;
    }


    public async Task SendKeybind(TasKeybind keybind) {
        Console.WriteLine($"Sending keybind {keybind}");
        var data = new[] { (byte)keybind };
        await SendToAll((byte)ServerOpCode.KeybindTriggered, data);
    }

    public async Task SendPath(string? path) {
        Console.WriteLine($"Path is {path ?? "<null>"}");
        await SendToAll((byte)ServerOpCode.SendPath, path ?? "");
    }

    protected override async Task<bool> ProcessRequest(byte opcodeByte, byte[] request) {
        switch ((ClientOpCode)opcodeByte) {
            case ClientOpCode.EstablishConnection:
                _viewModel.ConnectionState = "Connected";
                await SendPath(_viewModel.CurrentFilePath);
                break;
            case ClientOpCode.SetInfoString:
                _viewModel.InfoText = Encoding.UTF8.GetString(request);
                break;
            case ClientOpCode.SetStudioInfo:
                var info = StudioInfo.FromByteArray(request);
                _viewModel.StudioInfo = info.CurrentLine == -1 ? null : info;
                break;
            case ClientOpCode.CloseConnection:
                return true;
            default:
                _viewModel.ConnectionState = $"Unexpected opcode {opcodeByte}";
                break;
        }

        return false;
    }

    protected override void OnClosedConnection(TcpClient client, bool gracefully) {
        if (_viewModel.ConnectionState == "Connected") {
            _viewModel.ConnectionState = gracefully ? "Searching..." : "Searching... (Closed without message)";
            _viewModel.InfoText = "";
        }
    }
}