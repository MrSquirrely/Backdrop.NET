using System.Text;
using System.Timers;

using Microsoft.Win32;

using WpfScreenHelper;
using Timer = System.Timers.Timer;

namespace Backdrop.NET;

public class WindowStateChecker {
    public enum WindowState { Maximized, NotMaximized }

    private readonly Dictionary<string, WindowState> globalCacheScreenState = new();
    private readonly Timer timer;
    private bool isChecking;
    private static bool isLocked;

    public event Action<WindowState, Screen>? WindowStateChanged;

    static WindowStateChecker() {
        SystemEvents.SessionSwitch += (s, e) => {
	        isLocked = e.Reason switch {
		        SessionSwitchReason.SessionLock => true,
		        SessionSwitchReason.SessionUnlock => false,
		        _ => isLocked
	        };
        };
    }

    public WindowStateChecker(double intervalMilliseconds = 1000) {
        timer = new Timer(intervalMilliseconds);
        timer.Elapsed += CheckWindowState;
    }

    public void Start() => timer.Start();
    public void Stop() {
        timer.Stop();
        globalCacheScreenState.Clear();
    }

    private void CheckWindowState(object? source, ElapsedEventArgs e) {
        if (isChecking) return;
        isChecking = true;

        try {
            if (WindowStateChanged == null) return;

            List<IntPtr> allWindows = new List<IntPtr>();
            DesktopApi.EnumWindows((hwnd, lParam) => {
                allWindows.Add(hwnd);
                return true;
            }, IntPtr.Zero);

            Dictionary<string, WindowState> currentScreenStates = Screen.AllScreens
                .ToDictionary(s => s.DeviceName, _ => WindowState.NotMaximized);

            foreach (IntPtr hwnd in allWindows) {
                Screen? screen = Screen.FromHandle(hwnd);
                if (string.IsNullOrEmpty(screen.DeviceName) || !currentScreenStates.ContainsKey(screen.DeviceName)) continue;
                if (currentScreenStates[screen.DeviceName] == WindowState.Maximized) continue;

                if (IsWindowMaximized(hwnd)) {
                    currentScreenStates[screen.DeviceName] = WindowState.Maximized;
                }
            }

            foreach (KeyValuePair<string, WindowState> item in currentScreenStates) {
                Screen? screen = Screen.AllScreens.FirstOrDefault(m => m.DeviceName == item.Key);
                if (screen == null) continue;

                if (!globalCacheScreenState.TryGetValue(item.Key, out WindowState previousState) || item.Value != previousState) {
                    globalCacheScreenState[item.Key] = item.Value;
                    WindowStateChanged?.Invoke(item.Value, screen);
                }
            }
        }
        catch (Exception) {
            // Log exceptions here if logging is attached
        }
        finally {
            isChecking = false;
        }
    }

    private bool IsWindowMaximized(IntPtr hWnd) {
        if (isLocked) return true;
        if (DesktopApi.IsIconic(hWnd) || !DesktopApi.IsWindowVisible(hWnd)) return false;

        if (DesktopApi.DwmGetWindowAttribute(hWnd, DesktopApi.DWMWA_CLOAKED, out int cloakedVal, sizeof(int)) == 0 && cloakedVal != 0) {
            return false;
        }

        StringBuilder className = new StringBuilder(256);
        if (DesktopApi.GetClassName(hWnd, className, 256) > 0) {
            string cls = className.ToString();
            if (cls == "WorkerW" || cls == "Progman" || cls == "mpv") return false;
        }

        if (DesktopApi.IsZoomed(hWnd)) return true;

        if (DesktopApi.GetWindowRect(hWnd, out DesktopApi.RECT rect)) {
            Screen? screen = Screen.FromHandle(hWnd);
            double windowArea = rect.Width * rect.Height;
            double screenArea = screen.Bounds.Width * screen.Bounds.Height;
            if (screenArea > 0 && (windowArea / screenArea) >= 0.95) {
                return true;
            }
        }

        return false;
    }
}