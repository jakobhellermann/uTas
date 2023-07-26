using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using TasEditor.Services;
using TasEditor.ViewModels;
using TasEditor.Views;

namespace TasEditor;

public class App : Application {
    private const int Port = 34729;
    private IClientCommunicationService _clientCommunicationService = null!;

    public static SettingsService SettingsService { get; } = new();

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        var cancellationTokenSource = new CancellationTokenSource();

        var settings = SettingsService.Settings;
        var viewModel = new MainViewModel(new DummyClientCommunicationService()) {
            CurrentFilePath = settings.CurrentFile
        };

        if (!Design.IsDesignMode) {
            _clientCommunicationService = new ClientCommunicationService(viewModel);
            viewModel.ClientCommunicationService = _clientCommunicationService;
            _ = Task.Run(async () => {
                try {
                    await _clientCommunicationService.Start(IPAddress.Any, Port, cancellationTokenSource.Token);
                } catch (OperationCanceledException) {
                } catch (Exception e) {
                    Console.WriteLine(e);
                    throw;
                }
            });
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow {
                DataContext = viewModel
            };
            desktop.Exit += (_, _) => {
                cancellationTokenSource.Cancel();
                _clientCommunicationService.Dispose();
            };
        } else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
            singleViewPlatform.MainView = new MainView {
                DataContext = viewModel
            };
        }


        base.OnFrameworkInitializationCompleted();
    }
}