using System;
using System.Windows;
using System.Windows.Interop;

namespace Backdrop.NET;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class BackgroundWindow : Window {

	public int MonitorIndex { get; private set; }
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

		// Push the window to the absolute bottom of the Z-order
		IntPtr HWND_BOTTOM = new IntPtr(1);
		uint SWP_NOMOVE = 0x0002;
		uint SWP_NOSIZE = 0x0001;
		uint SWP_NOACTIVATE = 0x0010;

		DesktopApi.SetWindowPos(handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
	}

}
