using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TasEditor.Services;
using uTas.Communication;

namespace TasEditor.ViewModels;

public partial class MainViewModel : ObservableObject {
    [ObservableProperty] private double _fontSize = 14.0;

    [ObservableProperty] private string _connectionState = "Searching...";

    [ObservableProperty] private string _infoText = "";

    [ObservableProperty] private bool _frameByFrameEditorOpen;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CurrentFileName))]
    private string? _currentFilePath;

    public string? CurrentFileName => CurrentFilePath == null ? null : Path.GetFileName(CurrentFilePath);

    [ObservableProperty] private StudioInfo? _studioInfo;

    public bool EditorTextDirty = false;

    partial void OnCurrentFilePathChanged(string? value) {
        ClientCommunicationService.SendPath(value);
    }

    public void IncreaseFontSize() {
        FontSize += 1;
    }

    public void DecreaseFontSize() {
        FontSize -= 1;
    }

    public IClientCommunicationService ClientCommunicationService;

    public void StartStop() {
        Task.Run(async () => { await ClientCommunicationService.SendKeybind(TasKeybind.StartStop); });
    }

    public void FrameAdvance() {
        Task.Run(async () => { await ClientCommunicationService.SendKeybind(TasKeybind.FrameAdvance); });
    }

    public void PauseResume() {
        Task.Run(async () => { await ClientCommunicationService.SendKeybind(TasKeybind.PauseResume); });
    }

    public void ToggleHitboxes() {
        Task.Run(async () => { await ClientCommunicationService.SendKeybind(TasKeybind.ToggleHitboxes); });
    }


    public void RemoveAllBreakpoints() {
        Console.WriteLine("Removing all breakpoints");
    }

    public void OpenFrameByFrameEditor() {
        FrameByFrameEditorOpen = true;
    }

    public void CloseFrameByFrameEditor() {
        FrameByFrameEditorOpen = false;
    }


    public MainViewModel(IClientCommunicationService clientCommunicationService) {
        ClientCommunicationService = clientCommunicationService;
    }

    public MainViewModel() {
        ClientCommunicationService = new DummyClientCommunicationService();
    }
}