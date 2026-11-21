using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

using ManagedShell.Common.Helpers;

namespace Backdrop.NET.Shell;

/// <summary>
/// The Settings app: an ordinary window (it shows on the bar like any other app) for the shell's options.
/// </summary>
public partial class SettingsWindow : Window {

    private static SettingsWindow? current;

    // True while the controls are being filled in from the settings, so those changes are not written back
    private bool refreshing;

    public SettingsWindow() {
        InitializeComponent();

        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Backdrop.NET {version?.ToString(3) ?? string.Empty}";

        ShellSettings.Changed += OnSettingsChanged;
        Closed += (_, _) => {
            ShellSettings.Changed -= OnSettingsChanged;
            current = null;
        };

        Refresh();
    }

    /// <summary>Opens the Settings window, or brings the one that is already open to the front.</summary>
    public static void ShowSingle() {
        if (current is null) {
            current = new SettingsWindow();
            current.Show();
        }

        if (current.WindowState == WindowState.Minimized) {
            current.WindowState = WindowState.Normal;
        }

        current.Activate();
    }

    private void OnSettingsChanged(object? sender, EventArgs e) {
        Refresh();
    }

    // Also runs when a setting is changed elsewhere (the bar's menu, Quick Settings), to stay in step
    private void Refresh() {
        refreshing = true;

        try {
            ShellSettings settings = ShellSettings.Current;
            StyleBox.SelectedIndex = (int)settings.Style;
            AutoHideToggle.IsOn = settings.AutoHide;
            StyleDescription.Text = settings.Style switch {
                TaskbarStyle.Islands => "Three floating pieces: Start at the far left, tasks in the middle, notifications at the far right.",
                _ => "One solid bar: Start on the left, tasks beside it, notifications on the right."
            };
        }
        finally {
            refreshing = false;
        }
    }

    #region Events

    private void StyleBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (refreshing || (StyleBox.SelectedIndex < 0)) {
            return;
        }

        TaskbarStyle style = (TaskbarStyle)StyleBox.SelectedIndex;
        ShellSettings.Update(s => s.Style = style);
    }

    private void AutoHideToggle_Toggled(object sender, RoutedEventArgs e) {
        if (refreshing) {
            return;
        }

        bool autoHide = AutoHideToggle.IsOn;
        ShellSettings.Update(s => s.AutoHide = autoHide);
    }

    private void OpenSettingsFolder_Click(object sender, RoutedEventArgs e) {
        OpenFolder(Path.GetDirectoryName(ShellSettings.SettingsFilePath)!);
    }

    private void OpenWidgetsFolder_Click(object sender, RoutedEventArgs e) {
        OpenFolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Widgets"));
    }

    private void OpenLog_Click(object sender, RoutedEventArgs e) {
        if (File.Exists(Log.LogFilePath)) {
            ShellHelper.StartProcess(Log.LogFilePath);
        }
        else {
            iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("Nothing has been logged yet.", "Backdrop.NET",
                                                  MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private static void OpenFolder(string path) {
        try {
            Directory.CreateDirectory(path);
            ShellHelper.StartProcess(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Error($"Could not open folder {path}", ex);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) {
        App.ExitShell();
    }

    #endregion

}