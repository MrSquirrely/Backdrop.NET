using System.Windows;

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
}