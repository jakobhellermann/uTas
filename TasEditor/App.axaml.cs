using System;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using TasEditor.Communication;
using TasEditor.ViewModels;
using TasEditor.Views;

namespace TasEditor;

public partial class App : Application {
    private const int Port = 34729;
    private TasCommServer _server = null!;

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        var viewModel = new MainViewModel();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow {
                DataContext = viewModel
            };
            desktop.Exit += (_, _) => { _server.Dispose(); };
        } else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
            singleViewPlatform.MainView = new MainView {
                DataContext = viewModel
            };
        }

        if (!Avalonia.Controls.Design.IsDesignMode) {
            _server = new TasCommServer(viewModel);
            viewModel.TasCommServer = _server;
            _ = Task.Run(async () => {
                try {
                    await _server.Start(IPAddress.Any, Port);
                } catch (Exception e) {
                    Console.WriteLine(e);
                    throw;
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
}