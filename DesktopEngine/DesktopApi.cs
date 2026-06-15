using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Backdrop.NET;

public static class DesktopApi {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    // Keep 32-bit signatures available (used for fallback)
    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, bool pvParam, uint fWinIni);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int MapWindowPoints(IntPtr hwndFrom, IntPtr hwndTo, ref RECT lpPoints, uint cPoints);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, uint dwAttribute, out int pvAttribute, int cbAttribute);

    // Pointer-size (x64-safe) Get/SetWindowLongPtr wrappers.
    // On x64 these map to GetWindowLongPtr/SetWindowLongPtr, on x86 we use the existing GetWindowLong/SetWindowLong.
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) =>
        IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong(hWnd, nIndex));

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) {
        if (IntPtr.Size == 8) {
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        } else {
            // fallback to 32-bit SetWindowLong
            int res = SetWindowLong(hWnd, nIndex, dwNewLong.ToInt32());
            return new IntPtr(res);
        }
    }

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;

        public RECT(int left, int top, int right, int bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;
    public const int WS_CHILD = 0x40000000;
    public const int WS_POPUP = unchecked((int)0x80000000);
    public const int WS_CAPTION = 0x00C00000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;

    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_FRAMECHANGED = 0x0020;
    public const uint SWP_NOACTIVATE = 0x0010;

    public const uint SPI_SETCLIENTAREAANIMATION = 0x1043;
    public const uint SPIF_UPDATEINIFILE = 0x01;
    public const uint SPIF_SENDWININICHANGE = 0x02;
    public const uint DWMWA_CLOAKED = 14;

    public static void SendToBackground(IntPtr myWindowHandle, int x, int y, int width, int height) {
        if (myWindowHandle == IntPtr.Zero) return;

        // Pre-position out of view to avoid activation
        SetWindowPos(myWindowHandle, IntPtr.Zero, -10000, 0, 0, 0, SWP_NOACTIVATE | SWP_NOZORDER);

        IntPtr progman = FindWindow("Progman", null);
        IntPtr result = IntPtr.Zero;
        SendMessageTimeout(progman, 0x052C, new IntPtr(0x0000000D), IntPtr.Zero, 0x0000, 1000, out result);

        IntPtr workerW = FindDesktopWorkerW(progman);
        if (workerW == IntPtr.Zero) {
            SystemParametersInfo(SPI_SETCLIENTAREAANIMATION, 0, true, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
            workerW = FindDesktopWorkerW(progman);
        }

        if (workerW == IntPtr.Zero) return;

        // Read/modify styles using pointer-sized APIs
        IntPtr currentStylePtr = GetWindowLongPtr(myWindowHandle, GWL_STYLE);
        long currentStyle = currentStylePtr.ToInt64();
        long newStyle = (currentStyle & ~((long)WS_POPUP) & ~((long)WS_CAPTION)) | ((long)WS_CHILD);
        SetWindowLongPtr(myWindowHandle, GWL_STYLE, new IntPtr(newStyle));

        IntPtr currentExStylePtr = GetWindowLongPtr(myWindowHandle, GWL_EXSTYLE);
        long currentExStyle = currentExStylePtr.ToInt64();
        long newExStyle = currentExStyle | ((long)WS_EX_TOOLWINDOW);
        SetWindowLongPtr(myWindowHandle, GWL_EXSTYLE, new IntPtr(newExStyle));

        IntPtr parentResult = IntPtr.Zero;
        int attempts = 0;
        while (parentResult == IntPtr.Zero && attempts < 50) {
            parentResult = SetParent(myWindowHandle, workerW);
            if (parentResult == IntPtr.Zero) System.Threading.Thread.Sleep(100);
            attempts++;
        }

        if (parentResult == IntPtr.Zero) {
            // diagnostics enabled by default: log Win32 error code when SetParent fails
            int err = Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine($"SetParent failed: {err}");
            return;
        }

        RECT geometryBounds = new RECT(x, y, x + width, y + height);
        MapWindowPoints(IntPtr.Zero, workerW, ref geometryBounds, 2);

        SetWindowPos(myWindowHandle, IntPtr.Zero, geometryBounds.Left, geometryBounds.Top, geometryBounds.Width, geometryBounds.Height, SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    private static IntPtr FindDesktopWorkerW(IntPtr progman) {
        IntPtr workerW = IntPtr.Zero;
        EnumWindows((toplevelHandle, lParam) => {
            IntPtr shellDll = FindWindowEx(toplevelHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellDll != IntPtr.Zero) {
                workerW = FindWindowEx(IntPtr.Zero, toplevelHandle, "WorkerW", null);
            }
            return true;
        }, IntPtr.Zero);

        if (workerW == IntPtr.Zero) {
            IntPtr child = IntPtr.Zero;
            while ((child = FindWindowEx(progman, child, "WorkerW", null)) != IntPtr.Zero) {
                if (FindWindowEx(child, IntPtr.Zero, "SHELLDLL_DefView", null) == IntPtr.Zero) {
                    workerW = child;
                    break;
                }
            }
        }
        return workerW;
    }
}
