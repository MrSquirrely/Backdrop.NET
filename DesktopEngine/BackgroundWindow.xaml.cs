using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Backdrop.NET;

/// <summary>
/// The desktop layer for one monitor. Pinned to that monitor's physical bounds and to the bottom of the Z-order.
/// </summary>
public partial class BackgroundWindow : Window {

    public int MonitorIndex { get; }
    private readonly int x;
    private readonly int y;
    private readonly int width;
    private readonly int height;

    public BackgroundWindow(int monitorIndex, int x, int y, int width, int height) {
        InitializeComponent();
        MonitorIndex = monitorIndex;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
        SourceInitialized += OnWindowSourceInitialized;
    }

    private void OnWindowSourceInitialized(object? sender, EventArgs e) {
        IntPtr handle = new WindowInteropHelper(this).Handle;

        // Keep the desktop layer out of Alt+Tab
        long exStyle = DesktopApi.GetWindowLongPtr(handle, DesktopApi.GWL_EXSTYLE).ToInt64();
        DesktopApi.SetWindowLongPtr(handle, DesktopApi.GWL_EXSTYLE, new IntPtr(exStyle | DesktopApi.WS_EX_TOOLWINDOW));

        HwndSource.FromHwnd(handle)?.AddHook(WndProc);

        // Place the window in physical pixels; the hook below keeps it there
        DesktopApi.SetWindowPos(handle, DesktopApi.HWND_BOTTOM, x, y, width, height, DesktopApi.SWP_NOACTIVATE);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
        if (msg == DesktopApi.WM_WINDOWPOSCHANGING) {
            // Whatever asks to move, resize or raise this window (activation, WPF, a DPI change),
            // it stays on its monitor and at the bottom of the Z-order.
            DesktopApi.WINDOWPOS pos = Marshal.PtrToStructure<DesktopApi.WINDOWPOS>(lParam);
            pos.hwndInsertAfter = DesktopApi.HWND_BOTTOM;
            pos.x = x;
            pos.y = y;
            pos.cx = width;
            pos.cy = height;
            pos.flags &= ~(DesktopApi.SWP_NOZORDER | DesktopApi.SWP_NOMOVE | DesktopApi.SWP_NOSIZE);
            Marshal.StructureToPtr(pos, lParam, false);
        }

        return IntPtr.Zero;
    }

    protected override void OnClosing(CancelEventArgs e) {
        // Alt+F4 must not close the desktop. Application.Shutdown() ignores this cancel, so exiting still works.
        e.Cancel = true;
        base.OnClosing(e);
    }

}