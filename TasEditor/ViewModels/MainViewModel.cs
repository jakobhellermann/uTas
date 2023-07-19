using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TasEditor.Communication;

namespace TasEditor.ViewModels;

public partial class MainViewModel : ObservableObject {
    [ObservableProperty] private double _fontSize = 14.0;

    [ObservableProperty] private string _connectionState = "Searching...";

    [ObservableProperty] private string _infoText = "";

    [ObservableProperty] private bool _frameByFrameEditorOpen;

    public string? CurrentFilePath = null;

    [ObservableProperty] private string? _currentFileName;

    public bool EditorTextDirty = false;

    public void IncreaseFontSize() {
        FontSize += 1;
    }

    public void DecreaseFontSize() {
        FontSize -= 1;
    }

    public TasCommServer? TasCommServer = null;


    public void StartStop() {
        if (TasCommServer is null) return;

        Task.Run(async () => { await TasCommServer.SendKeybind(TasKeybind.StartStop); });
    }

    public void FrameAdvance() {
        if (TasCommServer is null) return;

        Task.Run(async () => { await TasCommServer.SendKeybind(TasKeybind.FrameAdvance); });
    }

    public void PauseResume() {
        if (TasCommServer is null) return;

        Task.Run(async () => { await TasCommServer.SendKeybind(TasKeybind.PauseResume); });
    }

    public void ToggleHitboxes() {
        if (TasCommServer is null) return;

        Task.Run(async () => { await TasCommServer.SendKeybind(TasKeybind.ToggleHitboxes); });
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
}