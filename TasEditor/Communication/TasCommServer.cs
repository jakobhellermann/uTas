using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Communication;
using TasEditor.ViewModels;

namespace TasEditor.Communication;

public class TasCommServer : TasCommServerBase {
    private readonly MainViewModel _viewModel;

    public TasCommServer(MainViewModel viewModel) {
        _viewModel = viewModel;
    }

    public async ValueTask SendKeybind(TasKeybind keybind) {
        Console.WriteLine($"Sending keybind {keybind}");
        var data = new[] { (byte)keybind };
        await SendToAll((byte)ServerOpCode.KeybindTriggered, data);
    }

    protected override void ProcessRequest(byte opcodeByte, byte[] request) {
        switch ((ClientOpCode)opcodeByte) {
            case ClientOpCode.EstablishConnection:
                _viewModel.ConnectionState = "Connected";
                break;
            case ClientOpCode.SetInfoString:
                _viewModel.InfoText = Encoding.UTF8.GetString(request);
                break;
            case ClientOpCode.CloseConnection:
                _viewModel.ConnectionState = "Searching...";
                _viewModel.InfoText = "";
                break;
            default:
                _viewModel.ConnectionState = $"Unexpected opcode {opcodeByte}";
                break;
        }
    }

    protected override void OnClosedConnection(TcpClient client) {
        if (_viewModel.ConnectionState == "Connected") {
            _viewModel.ConnectionState = "Searching... (Closed without message)";
            _viewModel.InfoText = "";
        }
    }
}