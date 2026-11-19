using System.Runtime.InteropServices;

namespace Backdrop.NET;

public static class DpiHelper {

    // Must match the native POINT (two 32-bit ints): MonitorFromPoint takes it by value.
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint(int x, int y) {
        public readonly int X = x;
        public readonly int Y = y;
    }

    public enum MonitorDpiType {
        MdtEffectiveDpi = 0,
        MdtAngularDpi = 1,
        MdtRawDpi = 2,
        MdtDefault = MdtEffectiveDpi
    }

    private const int MonitorDefaultToNearest = 2;

    [DllImport("User32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint pt, int flags);

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    /// <summary>
    /// Gets the DPI scaling factors (X and Y) for the monitor containing the given physical pixel.
    /// </summary>
    public static (double ScaleX, double ScaleY) GetMonitorScaleFactor(int x, int y) {
        IntPtr hMonitor = MonitorFromPoint(new NativePoint(x, y), MonitorDefaultToNearest);

        if (hMonitor != IntPtr.Zero) {
            int result = GetDpiForMonitor(hMonitor, MonitorDpiType.MdtEffectiveDpi, out uint dpiX, out uint dpiY);

            if ((result == 0) && (dpiX > 0) && (dpiY > 0)) {
                return (dpiX / 96.0, dpiY / 96.0);
            }
        }

        return (1.0, 1.0);
    }
}