using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Backdrop.NET;

public static class DpiHelper {

    [StructLayout(LayoutKind.Sequential)]
    public struct SPoint(double x, double y) {
        public double X = x;
        public double Y = y;
    }

    public enum MonitorDpiType {
        MdtEffectiveDpi = 0,
        MdtAngularDpi = 1,
        MdtRawDpi = 2,
        MdtDefault = MdtEffectiveDpi
    }

    private const int MonitorDefaultToNearest = 2;

    [DllImport("User32.dll")]
    private static extern IntPtr MonitorFromPoint(SPoint pt, int flags);

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    /// <summary>
    /// Gets the DPI scaling factors (X and Y) for a specific screen.
    /// </summary>
    public static (double ScaleX, double ScaleY) GetMonitorScaleFactor(double x, double y) {
        try {
            IntPtr hMonitor = MonitorFromPoint(new SPoint(x, y), MonitorDefaultToNearest);

            if (hMonitor != IntPtr.Zero) {
                int result = GetDpiForMonitor(hMonitor, MonitorDpiType.MdtEffectiveDpi, out uint dpiX, out uint dpiY);

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
