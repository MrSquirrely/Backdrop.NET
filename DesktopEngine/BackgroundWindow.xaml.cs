using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Backdrop.NET;

/// <summary>
/// The desktop layer for one monitor. Pinned to that monitor's physical bounds and placed at the bottom
/// of the Z-order, where it stays: nothing (activation included) is allowed to reorder it afterwards.
/// </summary>
public partial class BackgroundWindow : Window {

    public int MonitorIndex { get; }
    private readonly int x;
    private readonly int y;
    private readonly int width;
    private readonly int height;
    private bool placingAtBottom;

    public BackgroundWindow(int monitorIndex, int x, int y, int width, int height) {
        InitializeComponent();
        MonitorIndex = monitorIndex;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
        SourceInitialized += OnWindowSourceInitialized;
    }

    /// <summary>
    /// Shows the "DEBUGGING" watermark in the middle of this screen with the given details under it.
    /// </summary>
    public void ShowDebugWatermark(string details) {
        DebugWatermarkText.Text = details;
        DebugWatermark.Visibility = Visibility.Visible;
    }

    private void OnWindowSourceInitialized(object? sender, EventArgs e) {
        IntPtr handle = new WindowInteropHelper(this).Handle;

        // Keep the desktop layer out of Alt+Tab
        long exStyle = DesktopApi.GetWindowLongPtr(handle, DesktopApi.GWL_EXSTYLE).ToInt64();
        DesktopApi.SetWindowLongPtr(handle, DesktopApi.GWL_EXSTYLE, new IntPtr(exStyle | DesktopApi.WS_EX_TOOLWINDOW));

        HwndSource.FromHwnd(handle)?.AddHook(WndProc);

        // Place the window in physical pixels at the bottom; the hook below keeps it there
        placingAtBottom = true;
        DesktopApi.SetWindowPos(handle, DesktopApi.HWND_BOTTOM, x, y, width, height, DesktopApi.SWP_NOACTIVATE);
        placingAtBottom = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
        if (msg == DesktopApi.WM_WINDOWPOSCHANGING) {
            // Whatever asks to move, resize or raise this window (activation, WPF, a DPI change),
            // it stays on its monitor and keeps its place in the Z-order.
            DesktopApi.WINDOWPOS pos = Marshal.PtrToStructure<DesktopApi.WINDOWPOS>(lParam);
            pos.x = x;
            pos.y = y;
            pos.cx = width;
            pos.cy = height;
            pos.flags &= ~(DesktopApi.SWP_NOMOVE | DesktopApi.SWP_NOSIZE);

            if (placingAtBottom) {
                pos.hwndInsertAfter = DesktopApi.HWND_BOTTOM;
                pos.flags &= ~DesktopApi.SWP_NOZORDER;
            }
            else {
                // Not forced to HWND_BOTTOM here: that would drop it underneath Explorer's own desktop window
                pos.flags |= DesktopApi.SWP_NOZORDER;
            }

            Marshal.StructureToPtr(pos, lParam, false);
        }

        return IntPtr.Zero;
    }

    protected override void OnClosing(CancelEventArgs e) {
        if (App.IsDebugging) {
            // While debugging, Alt+F4 on the desktop ends the whole app.
            App.ExitShell();
        }
        else {
            // Alt+F4 must not close the desktop. Application.Shutdown() ignores this cancel, so exiting still works.
            e.Cancel = true;
        }

        base.OnClosing(e);
    }

}