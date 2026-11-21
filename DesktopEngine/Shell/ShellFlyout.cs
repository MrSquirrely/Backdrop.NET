using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using ManagedShell.AppBar;

namespace Backdrop.NET.Shell;

/// <summary>
/// Base for the panels that open above the bar (Start menu, Quick Settings). Handles placement, the
/// liquid open/close animation, and closing on Escape or when the panel loses focus.
/// </summary>
public abstract class ShellFlyout : Window {

    private DateTime lastClosedUtc = DateTime.MinValue;

    protected ShellFlyout() {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;

        // Text inside the panel follows the theme (a plain Window would default to black)
        SetResourceReference(ForegroundProperty, "SystemControlForegroundBaseHighBrush");

        Deactivated += (_, _) => CloseFlyout();
        PreviewKeyDown += OnPreviewKeyDownCore;
    }

    /// <summary>The panel inside the window that is animated. Leave a margin around it for the overshoot.</summary>
    protected abstract FrameworkElement AnimatedRoot { get; }

    /// <summary>True to open against the right end of the bar, false for the left end.</summary>
    protected abstract bool AlignRight { get; }

    // The animation grows out of the bottom corner nearest the button that opens the panel
    private Point Anchor => new(AlignRight ? 0.92 : 0.08, 1.0);

    public bool IsOpen { get; private set; }

    public event EventHandler? IsOpenChanged;

    public void Toggle(AppBarWindow bar) {
        if (IsOpen) {
            CloseFlyout();
        }
        // Clicking the bar button while the panel is open first closes it through Deactivated; don't reopen at once
        else if ((DateTime.UtcNow - lastClosedUtc) > TimeSpan.FromMilliseconds(250)) {
            Open(bar);
        }
    }

    public void Open(AppBarWindow bar) {
        // Sit just above the bar (PointToScreen is in pixels; Left/Top/Width are in DIPs)
        Point barTopLeft = bar.PointToScreen(new Point(0, 0));
        double barLeft = barTopLeft.X / bar.DpiScale;
        double barTop = barTopLeft.Y / bar.DpiScale;

        Left = AlignRight ? (barLeft + bar.ActualWidth - Width + 4) : (barLeft - 4);
        Top = barTop - Height + 4;

        OnOpening();
        Show();
        Activate();

        IsOpen = true;
        IsOpenChanged?.Invoke(this, EventArgs.Empty);
        LiquidAnimation.Show(AnimatedRoot, Anchor);
        OnOpened();
    }

    public void CloseFlyout() {
        if (!IsOpen) {
            return;
        }

        IsOpen = false;
        lastClosedUtc = DateTime.UtcNow;
        IsOpenChanged?.Invoke(this, EventArgs.Empty);

        LiquidAnimation.Hide(AnimatedRoot, Anchor, completed: () => {
            if (!IsOpen) {
                Hide();
            }
        });
    }

    /// <summary>Called before the panel is shown: refresh its contents here.</summary>
    protected virtual void OnOpening() { }

    /// <summary>Called once the panel is visible and focused.</summary>
    protected virtual void OnOpened() { }

    private void OnPreviewKeyDownCore(object sender, KeyEventArgs e) {
        if (e.Key == Key.Escape) {
            CloseFlyout();
            e.Handled = true;
        }
    }

    protected override void OnClosing(CancelEventArgs e) {
        // Alt+F4 only dismisses the panel; the window is reused. Application.Shutdown() ignores this cancel.
        e.Cancel = true;
        CloseFlyout();
        base.OnClosing(e);
    }

}