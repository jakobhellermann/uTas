using System;
using System.Text;
using Communication;
using TasEditor.ViewModels;

namespace TasEditor.Communication;

public class TasCommServer : TasCommServerBase {
    private readonly MainViewModel _viewModel;

    public TasCommServer(MainViewModel viewModel) {
        _viewModel = viewModel;
    }

    protected override (byte, byte[])? ProcessRequest(byte opcodeByte, byte[] request) {
        switch ((RequestOpCode)opcodeByte) {
            case RequestOpCode.EstablishConnection:
                _viewModel.ConnectionState = "Connected";
                break;
            case RequestOpCode.SetInfoString:
                _viewModel.InfoText = Encoding.UTF8.GetString(request);
                break;
            case RequestOpCode.CloseConnection:
                _viewModel.ConnectionState = "Searching...";
                _viewModel.InfoText = "";
                break;
            default:
                _viewModel.ConnectionState = $"Unexpected opcode {opcodeByte}";
                break;
        }

        return null;
    }
}