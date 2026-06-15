using System.Runtime.InteropServices;

namespace Backdrop.NET;

public static class DesktopApi {

	[DllImport("user32.dll")]
	public static extern IntPtr FindWindow(string className, string windowName);

	[DllImport("user32.dll")]
	public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint timeout, out IntPtr result);

	[DllImport("user32.dll")]
	public static extern IntPtr SetParent(IntPtr hwndChild, IntPtr hwndNewParent);

	[DllImport("user32.dll")]
	public static extern IntPtr FindWindowEx(IntPtr parentHwnd, IntPtr childAfter, string className, string windowTitle);

	public static void SendToBackground(IntPtr windowHandle) {
		IntPtr progman = FindWindow("Progman", null);

		// Send message 0x052C to Progman. This forces Windows to spawn a WorkerW window behind the desktop icons.
		SendMessageTimeout(progman, 0x052C, new IntPtr(0), IntPtr.Zero, 0x0, 1000, out _);

		IntPtr workerW = IntPtr.Zero;

		// Loop through all windows to find the correct WorkerW
		EnumWindows(new EnumWindowsProc((tophandle, topparamhandle) => {
			IntPtr p = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
			if (p != IntPtr.Zero) {
				// The WorkerW we want is the sibling of the window hosting SHELLDLL_DefView
				workerW = FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
			}
			return true;
		}), IntPtr.Zero);

		if (workerW != IntPtr.Zero) {
			SetParent(windowHandle, workerW);
		}
	}

	private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

}
