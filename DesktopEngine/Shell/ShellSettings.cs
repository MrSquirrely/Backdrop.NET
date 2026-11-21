using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backdrop.NET.Shell;

public enum TaskbarStyle {
    /// <summary>One solid bar: Start on the left, tasks beside it, notification area on the right.</summary>
    Standard,

    /// <summary>Three floating pieces: Start at the far left, tasks centred, notification area at the far right.</summary>
    Islands
}

/// <summary>
/// User options for the shell bar, stored in %AppData%\Backdrop.NET\settings.json.
/// </summary>
public class ShellSettings {

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string SettingsFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Backdrop.NET", "settings.json");

    public TaskbarStyle Style { get; set; } = TaskbarStyle.Standard;

    /// <summary>Hide the bar and menus until the mouse reaches the screen edge.</summary>
    public bool AutoHide { get; set; }

    public static ShellSettings Current { get; private set; } = new();

    /// <summary>Raised on the calling thread after <see cref="Update"/> changes a setting.</summary>
    public static event EventHandler? Changed;

    public static void Load() {
        try {
            if (File.Exists(SettingsFilePath)) {
                Current = JsonSerializer.Deserialize<ShellSettings>(File.ReadAllText(SettingsFilePath), SerializerOptions) ?? new();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) {
            Log.Error($"Could not read {SettingsFilePath}; using default settings", ex);
            Current = new();
        }
    }

    public static void Update(Action<ShellSettings> change) {
        change(Current);

        try {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(Current, SerializerOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Error($"Could not save {SettingsFilePath}", ex);
        }

        Changed?.Invoke(null, EventArgs.Empty);
    }

}