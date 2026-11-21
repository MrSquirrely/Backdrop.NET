using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Backdrop.NET.Shell;

/// <summary>
/// The Start menu: a searchable list of installed apps, plus the Settings and Power buttons.
/// </summary>
public partial class StartMenuWindow {

    private readonly ObservableCollection<StartMenuApp> apps = [];
    private readonly ICollectionView appsView;
    private bool appsRequested;

    public StartMenuWindow() {
        InitializeComponent();

        appsView = CollectionViewSource.GetDefaultView(apps);
        appsView.Filter = MatchesSearch;
        AppList.ItemsSource = appsView;
        UserName.Text = Environment.UserName;
        SleepItem.IsEnabled = PowerActions.CanSleep;
        DebugExitButton.Visibility = App.IsDebugging ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override FrameworkElement AnimatedRoot => MenuRoot;

    protected override bool AlignRight => false;

    protected override void OnOpening() {
        SearchBox.Clear();
    }

    protected override void OnOpened() {
        SearchBox.Focus();

        if (!appsRequested) {
            appsRequested = true;
            _ = LoadAppsAsync();
        }
    }

    private async Task LoadAppsAsync() {
        try {
            List<StartMenuApp> found = await Task.Run(StartMenuApp.FindAll);

            foreach (StartMenuApp app in found) {
                apps.Add(app);
            }

            // Icons have to be created on this thread; do it in small batches so the menu stays responsive
            int loaded = 0;
            foreach (StartMenuApp app in found) {
                app.LoadIcon();

                if ((++loaded % 6) == 0) {
                    await Dispatcher.Yield(DispatcherPriority.Background);
                }
            }
        }
        catch (Exception ex) {
            Log.Error("Could not load the Start menu app list", ex);
        }
    }

    private bool MatchesSearch(object item) {
        string query = SearchBox.Text.Trim();
        return (query.Length == 0)
               || ((item is StartMenuApp app) && app.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase));
    }

    private void Launch(StartMenuApp app) {
        CloseFlyout();
        app.Launch();
    }

    #region Events

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) {
        // Raised once while the XAML is still loading, before the list exists
        if (appsView is null) {
            return;
        }

        appsView.Refresh();

        if (AppList.Items.Count > 0) {
            AppList.SelectedIndex = 0;
        }
    }

    private void AppItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        if (sender is ListBoxItem { DataContext: StartMenuApp app }) {
            Launch(app);
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
        switch (e.Key) {
            case Key.Enter when AppList.SelectedItem is StartMenuApp app:
                Launch(app);
                e.Handled = true;
                break;
            case Key.Down when SearchBox.IsKeyboardFocusWithin && (AppList.Items.Count > 0):
                AppList.SelectedIndex = Math.Min(AppList.SelectedIndex + 1, AppList.Items.Count - 1);
                AppList.ScrollIntoView(AppList.SelectedItem);
                e.Handled = true;
                break;
            case Key.Up when SearchBox.IsKeyboardFocusWithin && (AppList.SelectedIndex > 0):
                AppList.SelectedIndex--;
                AppList.ScrollIntoView(AppList.SelectedItem);
                e.Handled = true;
                break;
        }
    }

    private void DebugExitButton_Click(object sender, RoutedEventArgs e) {
        App.ExitShell();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        SettingsWindow.ShowSingle();
    }

    private void PowerButton_Click(object sender, RoutedEventArgs e) {
        if (PowerButton.ContextMenu is ContextMenu menu) {
            menu.PlacementTarget = PowerButton;
            menu.Placement = PlacementMode.Top;
            menu.IsOpen = true;
        }
    }

    private void Lock_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        PowerActions.Lock();
    }

    private void SignOut_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        PowerActions.SignOut();
    }

    private void Sleep_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        PowerActions.Sleep();
    }

    private void Restart_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        PowerActions.Restart();
    }

    private void ShutDown_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        PowerActions.ShutDown();
    }

    #endregion

}