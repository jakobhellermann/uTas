using System;
using System.IO;
using System.Text.Json;

namespace TasEditor.Services;

public record AppSettings {
    public string? CurrentFile { get; set; }
}

public class SettingsService {
    private static string _settingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "uTAS");

    private static string _settingsPath = Path.Combine(_settingsDir, "settings.json");

    private AppSettings? _settings;

    public AppSettings Settings => _settings ??= LoadSettings();

    public void Save(AppSettings settings) {
        _settings = settings;

        var jsonString = JsonSerializer.Serialize(settings);
        Directory.CreateDirectory(_settingsDir);
        File.WriteAllText(_settingsPath, jsonString);
    }

    private AppSettings LoadSettings() {
        if (File.Exists(_settingsPath)) {
            var jsonString = File.ReadAllText(_settingsPath);
            var value = JsonSerializer.Deserialize<AppSettings>(jsonString);
            if (value == null) throw new Exception("Failed to load app settings");

            _settings = value;
            return value;
        }

        return new AppSettings();
    }
}