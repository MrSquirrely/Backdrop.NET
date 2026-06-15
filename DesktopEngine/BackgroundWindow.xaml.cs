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

		Loaded += OnWindowLoaded;
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e) {
		IntPtr handle = new WindowInteropHelper(this).Handle;
		//DesktopApi.SendToBackground(handle, x, y, width, height);
	}
}