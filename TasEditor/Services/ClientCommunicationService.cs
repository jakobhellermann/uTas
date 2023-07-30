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

    protected override void OnEstablishConnection() {
        _viewModel.ConnectionState = "Connected";
        Console.WriteLine("established connection: sending path");
        Task.Run(() => SendPath(_viewModel.CurrentFilePath));
    }

    protected override void OnCloseConnection() {
    }

    protected override void OnSetInfoText(string infoText) {
        _viewModel.InfoText = infoText;
    }

    protected override void OnSetStudioInfo(StudioInfo? info) {
        _viewModel.StudioInfo = info;
    }

    protected override void OnSendKeybindings() {
        throw new NotImplementedException();
    }

    protected override void OnAnyConnectionClosed(bool gracefully) {
        if (_viewModel.ConnectionState == "Connected") {
            _viewModel.ConnectionState = gracefully ? "Searching..." : "Searching... (Closed without message)";
            _viewModel.InfoText = "";
        }
    }
}