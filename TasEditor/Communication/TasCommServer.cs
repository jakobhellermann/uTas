using System;
using Communication;
using TasEditor.ViewModels;

namespace TasEditor.Communication;

public class TasCommServer : TasCommServerBase {
    private readonly MainViewModel _viewModel;

    public TasCommServer(MainViewModel viewModel) {
        _viewModel = viewModel;
    }

    protected override (byte, byte[]) ProcessRequest(byte opcodeByte, byte[] request) {
        _viewModel.LastCommOpcode = opcodeByte;
        return (0, Array.Empty<byte>());
    }
}