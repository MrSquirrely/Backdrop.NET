using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using ManagedShell;
using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.WindowsTasks;
using ManagedShell.WindowsTray;

namespace Backdrop.NET.Shell;

/// <summary>
/// The shell bar at the bottom of the screen. It is a registered AppBar: Windows shrinks the work area
/// so maximized apps stop at it, and it stays above normal windows. With auto-hide on it gives the
/// space back and slides in only when the mouse reaches the screen edge.
/// </summary>
public partial class TaskbarWindow {

    private const double BarHeight = 48;

    private readonly ShellManager shellManager;
    private readonly StartMenuWindow startMenu = new();
    private readonly QuickSettingsWindow quickSettings = new();
    private readonly DispatcherTimer clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer hideTimer = new() { Interval = TimeSpan.FromMilliseconds(450) };
    private bool barVisible = true;
    private bool trayOverflowOpen;

    public TaskbarWindow(ShellManager shellManager, AppBarScreen screen)
        : base(shellManager.AppBarManager, shellManager.ExplorerHelper, shellManager.FullScreenHelper, screen,
               AppBarEdge.Bottom, ShellSettings.Current.AutoHide ? AppBarMode.AutoHide : AppBarMode.Normal, BarHeight) {
        this.shellManager = shellManager;
        RequiresScreenEdge = true;

        InitializeComponent();

        TaskList.ItemsSource = shellManager.Tasks?.GroupedWindows;
        PinnedTrayIcons.ItemsSource = shellManager.NotificationArea?.PinnedIcons;
        UnpinnedTrayIcons.ItemsSource = shellManager.NotificationArea?.UnpinnedIcons;

        clockTimer.Tick += (_, _) => UpdateClock();
        hideTimer.Tick += OnHideTimerTick;
        startMenu.IsOpenChanged += (_, _) => OnPropertyChanged(nameof(AllowAutoHide));
        quickSettings.IsOpenChanged += (_, _) => OnPropertyChanged(nameof(AllowAutoHide));
        PropertyChanged += OnAppBarPropertyChanged;
        ShellSettings.Changed += OnSettingsChanged;
        Loaded += OnLoaded;

        ApplySettings();
        UpdateClock();
        clockTimer.Start();
    }

    private FrameworkElement[] Islands => [StartIsland, TasksIsland, TrayIsland];

    #region Style and auto-hide

    private void OnLoaded(object sender, RoutedEventArgs e) {
        // Pour the bar in on first show
        HideInstantly();
        SetBarVisible(true);
    }

    private void OnSettingsChanged(object? sender, EventArgs e) {
        ApplySettings();
    }

    private void ApplySettings() {
        ShellSettings settings = ShellSettings.Current;
        bool islands = settings.Style == TaskbarStyle.Islands;

        // Start from a fully shown bar; auto-hide is re-evaluated at the end
        hideTimer.Stop();
        LiquidAnimation.Reset(BarHost);
        foreach (FrameworkElement island in Islands) {
            LiquidAnimation.Reset(island);
        }
        barVisible = true;

        StandardBackground.Visibility = islands ? Visibility.Collapsed : Visibility.Visible;
        Sections.Margin = islands ? new Thickness(8, 4, 8, 6) : new Thickness(0);

        // Standard: [Start][tasks .........][tray]   Islands: [Start ...][tasks][... tray]
        StartColumn.Width = islands ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        TasksColumn.Width = islands ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        TrayColumn.Width = islands ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        TasksIsland.HorizontalAlignment = islands ? HorizontalAlignment.Center : HorizontalAlignment.Left;

        foreach (Border island in Islands.OfType<Border>()) {
            island.Background = islands ? (Brush)FindResource("ShellSurfaceBrush") : Brushes.Transparent;
            island.BorderBrush = islands ? (Brush)FindResource("ShellBorderBrush") : null;
            island.BorderThickness = new Thickness(islands ? 1 : 0);
            island.CornerRadius = new CornerRadius(islands ? 14 : 0);
            island.Padding = islands ? new Thickness(4, 0, 4, 0) : new Thickness(0);
        }

        StandardStyleItem.IsChecked = !islands;
        IslandsStyleItem.IsChecked = islands;
        AutoHideItem.IsChecked = settings.AutoHide;

        // Changing the mode makes the base class register or release the reserved screen space
        AppBarMode desiredMode = settings.AutoHide ? AppBarMode.AutoHide : AppBarMode.Normal;
        if (AppBarMode != desiredMode) {
            AppBarMode = desiredMode;
        }

        if (!IsOpening) {
            OnPropertyChanged(nameof(AllowAutoHide));
        }
    }

    protected override bool ShouldAllowAutoHide() {
        // Stay up while a flyout is open, as well as for the base reasons (mouse within, context menu, drag)
        return base.ShouldAllowAutoHide()
               && startMenu is not { IsOpen: true }
               && quickSettings is not { IsOpen: true };
    }

    // The base class raises this whenever something that affects auto-hide changes (mouse in or out, etc.)
    private void OnAppBarPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if ((e.PropertyName != nameof(AllowAutoHide)) || IsOpening) {
            return;
        }

        hideTimer.Stop();

        if (AllowAutoHide) {
            hideTimer.Start(); // hide after a short pause, so brushing past the edge does not flicker
        }
        else {
            SetBarVisible(true);
        }
    }

    private void OnHideTimerTick(object? sender, EventArgs e) {
        hideTimer.Stop();

        if (AllowAutoHide) {
            SetBarVisible(false);
        }
    }

    private void SetBarVisible(bool visible) {
        if (visible == barVisible) {
            return;
        }

        barVisible = visible;

        if (ShellSettings.Current.Style != TaskbarStyle.Islands) {
            // One solid bar: it rises and sinks as a single piece
            if (visible) {
                LiquidAnimation.Show(BarHost, LiquidAnimation.FromBottom);
            }
            else {
                LiquidAnimation.Hide(BarHost, LiquidAnimation.FromBottom);
            }

            return;
        }

        // Islands: each piece moves on its own, one after the other, like separate drops
        FrameworkElement[] islands = Islands;

        if (visible) {
            BarHost.Visibility = Visibility.Visible;

            for (int i = 0; i < islands.Length; i++) {
                LiquidAnimation.Show(islands[i], LiquidAnimation.FromBottom, TimeSpan.FromMilliseconds(i * 70));
            }
        }
        else {
            for (int i = 0; i < islands.Length; i++) {
                bool last = i == (islands.Length - 1);
                LiquidAnimation.Hide(islands[i], LiquidAnimation.FromBottom, TimeSpan.FromMilliseconds(i * 45),
                    last ? HideBarHostIfStillHidden : null);
            }
        }
    }

    // BarHost is what catches the mouse; once hidden, only the 2px edge sensor is left and apps get their clicks
    private void HideBarHostIfStillHidden() {
        if (!barVisible) {
            BarHost.Visibility = Visibility.Hidden;
        }
    }

    private void HideInstantly() {
        barVisible = false;

        if (ShellSettings.Current.Style == TaskbarStyle.Islands) {
            foreach (FrameworkElement island in Islands) {
                island.Visibility = Visibility.Hidden;
            }
        }
        else {
            BarHost.Visibility = Visibility.Hidden;
        }
    }

    #endregion

    #region Bar context menu

    private void StandardStyleItem_Click(object sender, RoutedEventArgs e) {
        ShellSettings.Update(static s => s.Style = TaskbarStyle.Standard);
    }

    private void IslandsStyleItem_Click(object sender, RoutedEventArgs e) {
        ShellSettings.Update(static s => s.Style = TaskbarStyle.Islands);
    }

    private void AutoHideItem_Click(object sender, RoutedEventArgs e) {
        ShellSettings.Update(static s => s.AutoHide = !s.AutoHide);
    }

    private void TaskManagerItem_Click(object sender, RoutedEventArgs e) {
        ShellHelper.StartTaskManager();
    }

    private void ExitItem_Click(object sender, RoutedEventArgs e) {
        App.ExitShell();
    }

    #endregion

    #region Start

    private void StartButton_Click(object sender, RoutedEventArgs e) {
        startMenu.Toggle(this);
    }

    private void QuickSettingsButton_Click(object sender, RoutedEventArgs e) {
        quickSettings.Toggle(this);
    }

    #endregion

    #region Tasks

    // Clicking the bar can take focus from the app first, so remember whether it was active before the click
    private void TaskButton_MouseEnter(object sender, MouseEventArgs e) {
        if (sender is Button { DataContext: ApplicationWindow window } button) {
            button.Tag = window.State == ApplicationWindow.WindowState.Active;
        }
    }

    private void TaskButton_Click(object sender, RoutedEventArgs e) {
        if (sender is not Button { DataContext: ApplicationWindow window } button) {
            return;
        }

        if (button.Tag is true && window.CanMinimize) {
            window.Minimize();
            button.Tag = false;
        }
        else {
            window.BringToFront();
            button.Tag = true;
        }
    }

    private void TaskMinimize_Click(object sender, RoutedEventArgs e) {
        if (sender is FrameworkElement { DataContext: ApplicationWindow window }) {
            window.Minimize();
        }
    }

    private void TaskClose_Click(object sender, RoutedEventArgs e) {
        if (sender is FrameworkElement { DataContext: ApplicationWindow window }) {
            window.Close();
        }
    }

    #endregion

    #region Notification area

    private void TrayOverflowButton_Click(object sender, RoutedEventArgs e) {
        trayOverflowOpen = !trayOverflowOpen;

        if (trayOverflowOpen) {
            LiquidAnimation.Show(UnpinnedTrayIcons, LiquidAnimation.FromRight);
        }
        else {
            LiquidAnimation.Hide(UnpinnedTrayIcons, LiquidAnimation.FromRight,
                completed: () => UnpinnedTrayIcons.Visibility = Visibility.Collapsed);
        }

        TrayOverflowButton.ToolTip = trayOverflowOpen ? "Hide icons" : "Show hidden icons";
        TrayOverflowRotation.BeginAnimation(RotateTransform.AngleProperty,
            new DoubleAnimation(trayOverflowOpen ? 180 : 0, TimeSpan.FromMilliseconds(250)) {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 }
            });
    }

    private static NotifyIcon? IconOf(object sender) {
        return (sender as FrameworkElement)?.DataContext as NotifyIcon;
    }

    private static int DoubleClickTime => (int)DesktopApi.GetDoubleClickTime();

    private void TrayIcon_MouseDown(object sender, MouseButtonEventArgs e) {
        IconOf(sender)?.IconMouseDown(e.ChangedButton, MouseHelper.GetCursorPositionParam(), DoubleClickTime);
        e.Handled = true;
    }

    private void TrayIcon_MouseUp(object sender, MouseButtonEventArgs e) {
        IconOf(sender)?.IconMouseUp(e.ChangedButton, MouseHelper.GetCursorPositionParam(), DoubleClickTime);
        e.Handled = true;
    }

    private void TrayIcon_MouseEnter(object sender, MouseEventArgs e) {
        if ((sender is not FrameworkElement element) || (IconOf(sender) is not NotifyIcon icon)) {
            return;
        }

        // Apps ask where their icon is (in screen pixels) to position their own flyouts
        Point topLeft = element.PointToScreen(new Point(0, 0));
        icon.Placement = new ManagedShell.Interop.NativeMethods.Rect {
            Left = (int)topLeft.X,
            Top = (int)topLeft.Y,
            Right = (int)(topLeft.X + (element.ActualWidth * DpiScale)),
            Bottom = (int)(topLeft.Y + (element.ActualHeight * DpiScale))
        };
        icon.IconMouseEnter(MouseHelper.GetCursorPositionParam());
    }

    private void TrayIcon_MouseLeave(object sender, MouseEventArgs e) {
        IconOf(sender)?.IconMouseLeave(MouseHelper.GetCursorPositionParam());
    }

    private void TrayIcon_MouseMove(object sender, MouseEventArgs e) {
        IconOf(sender)?.IconMouseMove(MouseHelper.GetCursorPositionParam());
    }

    // A right-click on a tray icon belongs to that icon's app, not to the bar's own menu
    private void TrayIcon_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
        e.Handled = true;
    }

    private void UpdateClock() {
        DateTime now = DateTime.Now;
        ClockTime.Text = now.ToString("t");
        ClockDate.Text = now.ToString("d");
    }

    #endregion

    /// <summary>
    /// Releases the reserved screen space and closes the bar and its menu. Call before shutting the app down.
    /// </summary>
    public void Shutdown() {
        clockTimer.Stop();
        hideTimer.Stop();
        ShellSettings.Changed -= OnSettingsChanged;
        AllowClose = true;
        Close();
    }

}