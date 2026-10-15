using System.IO;
using System.Text.Json;
using System.Windows;

namespace AssetManager.Infrastructure;

/// <summary>
/// Replaces <c>Properties.Settings</c> and its <c>WindowSizes</c> NameValueCollection.
///
/// The old app persisted window sizes through the .NET Framework application-settings
/// infrastructure, which needed a designer-generated class, a config section, and a
/// <c>SettingsLoaded</c> hook just to initialise the collection on first run. This writes a small
/// JSON file under %APPDATA% instead: same behaviour, no legacy configuration plumbing.
///
/// Every operation is best-effort. A corrupt or unreadable settings file costs the user their
/// remembered window sizes, which is not worth failing startup over.
/// </summary>
public sealed class UserSettingsService
{
    private readonly string _settingsPath;
    private UserSettings _settings = new();

    public UserSettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AssetManager");

        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;
            var json = File.ReadAllText(_settingsPath);
            _settings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch
        {
            _settings = new UserSettings();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, SerializerOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Losing a remembered window size is not worth surfacing to the user.
        }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    /// <summary>Restores a window's remembered size, and keeps it updated as the user resizes.</summary>
    public void TrackWindowSize(Window window)
    {
        var key = window.GetType().FullName;
        if (key is null) return;

        if (_settings.WindowSizes.TryGetValue(key, out var size) && size is { Width: > 0, Height: > 0 })
        {
            window.Width = size.Width;
            window.Height = size.Height;
        }

        window.SizeChanged += (_, _) =>
        {
            // WindowState changes report sizes that should not be persisted as the restored size.
            if (window.WindowState != WindowState.Normal) return;

            _settings.WindowSizes[key] = new WindowSize(window.Width, window.Height);
            Save();
        };
    }

    private sealed class UserSettings
    {
        public Dictionary<string, WindowSize> WindowSizes { get; set; } = new();
    }

    private sealed record WindowSize(double Width, double Height);
}
