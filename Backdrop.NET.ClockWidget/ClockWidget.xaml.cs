using System.Windows.Controls;
using System.Windows.Threading;

namespace Backdrop.NET.ClockWidget;
/// <summary>
/// Interaction logic for ClockWidget.xaml
/// </summary>
public partial class ClockWidget : UserControl {
    private readonly DispatcherTimer timer;

    public ClockWidget() {
        InitializeComponent();

        timer = new DispatcherTimer {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (_, _) => UpdateTime();

        // Run only while on screen, so a removed widget is not kept alive by its timer.
        // (Moving between monitors unloads and reloads the control.)
        Loaded += (_, _) => {
            UpdateTime();
            timer.Start();
        };
        Unloaded += (_, _) => timer.Stop();

        UpdateTime();
    }

    private void UpdateTime() {
        DateTime now = DateTime.Now;
        TimeText.Text = now.ToString("h:mm tt");
        DateText.Text = now.ToString("dddd, MMMM d");
    }
}