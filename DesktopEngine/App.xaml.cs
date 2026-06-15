using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Backdrop.NET.Models;
using Backdrop.NET.Widgets;
using WpfScreenHelper;

namespace Backdrop.NET;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App {

    private readonly Dictionary<int, BackgroundWindow?> backgroundWindows = new();
    private WindowStateChecker? stateChecker;

    protected override void OnStartup(StartupEventArgs e) {
	    base.OnStartup(e);

	    int monitorIndex = 0;
	    double virtualScreenLeft = Screen.AllScreens.Min(s => s.Bounds.Left);
	    double virtualScreenTop = Screen.AllScreens.Min(s => s.Bounds.Top);

	    foreach (Screen screen in Screen.AllScreens) {
		    (double scaleX, double scaleY) = DpiHelper.GetMonitorScaleFactor(screen.Bounds.Left, screen.Bounds.Top);

		    int physicsX = (int)(screen.Bounds.Left - virtualScreenLeft);
		    int physicsY = (int)(screen.Bounds.Top - virtualScreenTop);
		    int physicsWidth = (int)screen.Bounds.Width;
		    int physicsHeight = (int)screen.Bounds.Height;

		    BackgroundWindow window = new(monitorIndex, physicsX, physicsY, physicsWidth, physicsHeight) {
			    Left = physicsX / scaleX,
			    Top = physicsY / scaleY,
			    Width = physicsWidth / scaleX,
			    Height = physicsHeight / scaleY,
			    WindowStyle = WindowStyle.None,
			    ResizeMode = ResizeMode.NoResize
		    };

		    window.Show();
		    backgroundWindows.Add(monitorIndex, window);
		    monitorIndex++;
	    }

	    LoadAndDistributeWidgets();

	    // Initialize and listen to window visibility state changes
	    InitializeOptimizationTracker();
    }

    private void InitializeOptimizationTracker() {
	    stateChecker = new WindowStateChecker(1000);
	    stateChecker.WindowStateChanged += (state, screen) => {
		    // Marshal back to the WPF UI thread safely
		    Dispatcher.BeginInvoke(new Action(() => {
			    var targetPair = Screen.AllScreens
			                           .Select((s, idx) => new { Screen = s, Index = idx })
			                           .FirstOrDefault(p => p.Screen.DeviceName == screen.DeviceName);

			    if (targetPair != null && backgroundWindows.TryGetValue(targetPair.Index, out BackgroundWindow? win) && win != null) {
				    if (state == WindowStateChecker.WindowState.Maximized) {
					    // Optimize resources: Screen is hidden behind a maximized app
					    win.Visibility = Visibility.Collapsed;
				    }
				    else {
					    // Restore resources: Screen background is visible again
					    win.Visibility = Visibility.Visible;
				    }
			    }
		    }));
	    };
	    stateChecker.Start();
    }

    private void LoadAndDistributeWidgets() {
        LayoutManager manager = new();
        List<WidgetState> savedWidgets = manager.LoadLayout();

        if (savedWidgets.Count == 0) {
	        savedWidgets.Add(new WidgetState {
		        WidgetId = Guid.NewGuid(),
		        WidgetType = "Clock",
		        MonitorIndex = 0, // Spawn on primary monitor
		        X = 100,
		        Y = 100,
		        ZIndex = 1
	        });

	        manager.SaveLayout(savedWidgets);
        }

        foreach (WidgetState savedWidget in savedWidgets) {
            if (backgroundWindows.TryGetValue(savedWidget.MonitorIndex, out BackgroundWindow? targetWindow)) {
                WidgetContainer container = CreateWidgetContainer(savedWidget);

                Canvas.SetLeft(container, savedWidget.X);
                Canvas.SetTop(container, savedWidget.Y);
                Panel.SetZIndex(container, savedWidget.ZIndex);

                targetWindow?.WidgetCanvas.Children.Add(container);
            }
            else {
                if (!backgroundWindows.TryGetValue(0, out BackgroundWindow? primaryWindow)) {
                    continue;
                }

                WidgetContainer container = CreateWidgetContainer(savedWidget);
                Canvas.SetLeft(container, 0); // Reset X to ensure it's visible
                Canvas.SetTop(container, 0);  // Reset Y to ensure it's visible
                primaryWindow?.WidgetCanvas.Children.Add(container);
            }
        }
    }

    private WidgetContainer CreateWidgetContainer(WidgetState state) {
        WidgetContainer container = new();

        //todo: add some default states
        switch (state.WidgetType) {
            case "Clock":
                container.WidgetContentHost.Content = new ClockWidget();
                break;
            default:
	            container.WidgetContentHost.Content = new Border {
		            Background = Brushes.Crimson,
		            Width = 250,
		            Height = 100,
		            CornerRadius = new CornerRadius(10),
		            Child = new TextBlock {
			            Text = "I AM A WIDGET",
			            Foreground = Brushes.White,
			            FontSize = 24,
			            FontWeight = FontWeights.Bold,
			            HorizontalAlignment = HorizontalAlignment.Center,
			            VerticalAlignment = VerticalAlignment.Center
		            }
	            };
                break;
        }

        return container;
    }

    protected override void OnExit(ExitEventArgs e) {
	    stateChecker?.Stop();
	    base.OnExit(e);
    }
}

