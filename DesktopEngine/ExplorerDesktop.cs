using System.Text;

namespace Backdrop.NET;

/// <summary>
/// Explorer's own desktop (the wallpaper and icons), which exists only while explorer.exe is running.
/// </summary>
public static class ExplorerDesktop {

    public static bool IsPresent => DesktopApi.FindWindow("Progman", null!) != IntPtr.Zero;

    /// <summary>
    /// Pushes Explorer's desktop windows underneath everything else, so the shell's bottom-layer
    /// windows sit just above them instead of being hidden behind them.
    /// </summary>
    public static void SendToBottom() {
        List<IntPtr> desktopWindows = [];

        DesktopApi.EnumWindows((hWnd, _) => {
            StringBuilder className = new(32);

            if ((DesktopApi.GetClassName(hWnd, className, className.Capacity) > 0)
                && (className.ToString() is "Progman" or "WorkerW")) {
                desktopWindows.Add(hWnd);
            }

            return true;
        }, IntPtr.Zero);

        // EnumWindows runs top to bottom; sending each to the bottom in that order keeps their own order intact
        foreach (IntPtr hWnd in desktopWindows) {
            DesktopApi.SetWindowPos(hWnd, DesktopApi.HWND_BOTTOM, 0, 0, 0, 0,
                DesktopApi.SWP_NOMOVE | DesktopApi.SWP_NOSIZE | DesktopApi.SWP_NOACTIVATE);
        }
    }

}