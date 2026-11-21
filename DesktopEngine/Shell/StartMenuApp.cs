using System.ComponentModel;
using System.IO;
using System.Windows.Media;

using ManagedShell.Common.Enums;
using ManagedShell.Common.Helpers;
using ManagedShell.UWPInterop;

namespace Backdrop.NET.Shell;

/// <summary>
/// One entry in the Start menu: either a Start Menu shortcut (.lnk) or an installed Store app.
/// </summary>
public class StartMenuApp : INotifyPropertyChanged {

    private readonly string? shortcutPath;
    private readonly StoreApp? storeApp;
    private ImageSource? icon;

    private StartMenuApp(string name, string? shortcutPath, StoreApp? storeApp) {
        Name = name;
        this.shortcutPath = shortcutPath;
        this.storeApp = storeApp;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; }

    public ImageSource? Icon {
        get => icon;
        private set {
            icon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
        }
    }

    /// <summary>Loads the icon. Must run on the UI thread, since the image belongs to the thread that creates it.</summary>
    public void LoadIcon() {
        try {
            Icon = storeApp is not null
                ? storeApp.GetIconImageSource(IconSize.Large)
                : IconImageConverter.GetImageFromAssociatedIcon(shortcutPath!, IconSize.Large);
        }
        catch (Exception ex) {
            Log.Error($"Could not load the icon for '{Name}'", ex);
        }
    }

    public void Launch() {
        try {
            if (storeApp is not null) {
                ShellHelper.ActivateApplication(storeApp.AppUserModelId);
            }
            else {
                ShellHelper.StartProcess(shortcutPath!);
            }
        }
        catch (Exception ex) {
            Log.Error($"Could not start '{Name}'", ex);
        }
    }

    /// <summary>
    /// Finds every app to list, sorted by name. Slow (disk and package queries): call it off the UI thread.
    /// </summary>
    public static List<StartMenuApp> FindAll() {
        Dictionary<string, StartMenuApp> byName = new(StringComparer.OrdinalIgnoreCase);
        EnumerationOptions options = new() {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        };

        // The user's own shortcuts first, so they win over an all-users shortcut with the same name
        foreach (string root in new[] { ShellHelper.UsersStartMenuPath, ShellHelper.AllUsersStartMenuPath }) {
            try {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) {
                    continue;
                }

                foreach (string shortcut in Directory.EnumerateFiles(root, "*.lnk", options)) {
                    string name = Path.GetFileNameWithoutExtension(shortcut);

                    if (!name.Contains("uninstall", StringComparison.OrdinalIgnoreCase)) {
                        byName.TryAdd(name, new StartMenuApp(name, shortcut, null));
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                Log.Error($"Could not read Start Menu folder {root}", ex);
            }
        }

        try {
            StoreAppHelper.AppList.FetchApps();

            foreach (StoreApp app in StoreAppHelper.AppList) {
                if (!string.IsNullOrWhiteSpace(app.DisplayName)) {
                    byName.TryAdd(app.DisplayName, new StartMenuApp(app.DisplayName, null, app));
                }
            }
        }
        catch (Exception ex) {
            Log.Error("Could not list Store apps", ex);
        }

        return [.. byName.Values.OrderBy(static a => a.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

}