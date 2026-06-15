using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Backdrop.NET;

public static class DesktopApi {

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

	private const int GWL_STYLE = -16;
	private const int WS_CHILD = 0x40000000;
	private const int WS_CAPTION = 0x00C00000;

	public static void SendToBackground(IntPtr myWindowHandle) {
		IntPtr progman = FindWindow("Progman", null);
		IntPtr result = IntPtr.Zero;

		SendMessageTimeout(progman, 0x052C, new IntPtr(0x0000000D), IntPtr.Zero, 0x0000, 1000, out result);

		IntPtr workerW = IntPtr.Zero;

		EnumWindows(new EnumWindowsProc((toplevelHandle, lParam) => {
			IntPtr shellDll = FindWindowEx(toplevelHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
			if (shellDll != IntPtr.Zero) {
				// Gets the WorkerW directly behind the shell desktop icons
				workerW = FindWindowEx(IntPtr.Zero, toplevelHandle, "WorkerW", null);
			}
			return true;
		}), IntPtr.Zero);

		if ((workerW == IntPtr.Zero) || (myWindowHandle == IntPtr.Zero)) {
			Debug.WriteLine("Returning");
			return;
		}

		int currentStyle = GetWindowLong(myWindowHandle, GWL_STYLE);
		_ = SetWindowLong(myWindowHandle, GWL_STYLE, (currentStyle | WS_CHILD) & ~WS_CAPTION);

		SetParent(myWindowHandle, workerW);
	}
}