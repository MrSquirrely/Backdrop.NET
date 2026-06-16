using System.Windows.Controls;
using System.Windows.Threading;

namespace Backdrop.NET.Widgets;
/// <summary>
/// Interaction logic for ClockWidget.xaml
/// </summary>
public partial class ClockWidget : UserControl {

	private readonly DispatcherTimer timer;
    public ClockWidget() {
        InitializeComponent();

        UpdateTime();

        timer = new DispatcherTimer() {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (sender, args) => UpdateTime();
        timer.Start();
    }

    private void UpdateTime() {
	    TimeText.Text = DateTime.Now.ToString("h:mm tt");
	    DateText.Text = DateTime.Now.ToString("dddd, MMMM, d");
    }
}
