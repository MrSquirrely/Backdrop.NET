using System.Windows;
using System.Windows.Interop;

namespace Backdrop.NET;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class BackgroundWindow : Window {

    public int MonitorIndex { get; private set; }

    public BackgroundWindow(int monitorIndex) {
        InitializeComponent();
        MonitorIndex = monitorIndex;
    }

    protected override void OnSourceInitialized(EventArgs e) {
	    base.OnSourceInitialized(e);
	    IntPtr handle = new WindowInteropHelper(this).Handle;
	    DesktopApi.SendToBackground(handle);
    }
}