using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DesktopEngine;

public static class DpiHelper {

    [StructLayout(LayoutKind.Sequential)]
    public struct Point(int x, int y) {
        public int X = x;
        public int Y = y;
    }

    public enum MonitorDpiType {
        MDT_Effective_DPI = 0,
        MDT_Angular_DPI = 1,
        MDT_Raw_DPI = 2,
        MDT_Default = MDT_Effective_DPI
    }

    private const int MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("User32.dll")]
    private static extern IntPtr MonitorFromPoint(Point pt, int flags);

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    /// <summary>
    /// Gets the DPI scaling factors (X and Y) for a specific screen.
    /// </summary>
    public static (double ScaleX, double ScaleY) GetMonitorScaleFactor(double x, double y) {
        try {
            IntPtr hMonitor = MonitorFromPoint(new Point(x, y), MONITOR_DEFAULTTONEAREST);

            if (hMonitor != IntPtr.Zero) {
                int result = GetDpiForMonitor(hMonitor, MonitorDpiType.MDT_Effective_DPI, out uint dpiX, out uint dpiY);

                if (result == 0) {
                    return (dpiX / 96.0, dpiY / 96.0);
                }
            }
        }
        catch (DllNotFoundException e) {
            Debug.WriteLine(e);
            throw;
        }

        return (1.0, 1.0);
    }
}
