using CommunityToolkit.Mvvm.ComponentModel;

namespace TasEditor.ViewModels;

public partial class MainViewModel : ObservableObject {
    [ObservableProperty] private double _fontSize = 14.0;

    public void IncreaseFontSize() {
        FontSize += 1;
    }

    public void DecreaseFontSize() {
        FontSize -= 1;
    }
}