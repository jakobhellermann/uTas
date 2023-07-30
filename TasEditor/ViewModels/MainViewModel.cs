using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TasEditor.Services;
using uTas.Communication;

namespace TasEditor.ViewModels;

public partial class MainViewModel : ObservableObject {
    #region UI

    [ObservableProperty] private double _fontSize = 14.0;

    [ObservableProperty] private string _connectionState = "Searching...";

    [ObservableProperty] private string _infoText = "";

    [ObservableProperty] private bool _frameByFrameEditorOpen;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CurrentFileName))]
    private string? _currentFilePath;

    public string? CurrentFileName => CurrentFilePath == null ? null : Path.GetFileName(CurrentFilePath);

    private string? OldFilePath { get; set; }

#pragma warning disable CS0169 // used via OnPropertyChanged
    [ObservableProperty] private StudioInfo? _studioInfo;
#pragma warning restore CS0169

    public bool EditorTextDirty = false;

    partial void OnCurrentFilePathChanging(string? value) {
        OldFilePath = CurrentFilePath;
        ClientCommunicationService.SendPath(value);
    }

    public void IncreaseFontSize() {
        FontSize += 1;
    }

    public void DecreaseFontSize() {
        FontSize -= 1;
    }

    public void OpenLastFile() {
        if (OldFilePath is not null) CurrentFilePath = OldFilePath;
    }

    public void OpenFrameByFrameEditor() {
        FrameByFrameEditorOpen = true;
    }

    public void CloseFrameByFrameEditor() {
        FrameByFrameEditorOpen = false;
    }

    #endregion

    #region Editing

    private ITasEditingService _tasEditingService;

    public void ToggleCommentLineByLine() {
        _tasEditingService.ToggleCommentLineByLine();
    }

    public void ToggleComment() {
        _tasEditingService.ToggleComment();
    }

    public void CombineConsecutiveInputs() {
        _tasEditingService.CombineConsecutiveInputs();
    }

    #endregion

    #region Communication

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

    #endregion


    public MainViewModel(IClientCommunicationService clientCommunicationService, ITasEditingService tasEditingService) {
        ClientCommunicationService = clientCommunicationService;
        _tasEditingService = tasEditingService;
    }

    public MainViewModel() {
        _tasEditingService = new DummyTasEditingService();
        ClientCommunicationService = new DummyClientCommunicationService();
    }
}