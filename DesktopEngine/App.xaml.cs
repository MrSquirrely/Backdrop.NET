using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        int monitorIndex = 0;


        foreach (Screen screen in Screen.AllScreens) {

            (double scaleX, double scaleY) = DpiHelper.GetMonitorScaleFactor(screen.Bounds.Left, screen.Bounds.Top);

            BackgroundWindow window = new(monitorIndex) {
                Left = screen.Bounds.Left / scaleX,
                Top = screen.Bounds.Top / scaleY,
                Width = screen.Bounds.Width / scaleX,
                Height = screen.Bounds.Height / scaleY,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize
            };

#if DEBUG
	        TextBlock debugText = new() {
		        Text = $"Monitor {monitorIndex}",
		        FontSize = 150,
		        FontWeight = FontWeights.Bold,
		        Foreground = new SolidColorBrush(Color.FromArgb(75, 255, 255, 255)), // ~30% opacity white
		        IsHitTestVisible = false // Prevents the text from stealing mouse clicks
	        };

	        Canvas.SetLeft(debugText, 50);
	        Canvas.SetTop(debugText, 50);

	        window.WidgetCanvas.Children.Add(debugText);
#endif

            
            //window.SourceInitialized += (s, ev) => {
	           // IntPtr handle = new WindowInteropHelper(window).Handle;
            //    Debug.WriteLine("=======");
            //    Debug.WriteLine($"Source Initialized {handle}");
            //    Debug.WriteLine("=======");
            //    //DesktopApi.SendToBackground(handle);
            //    try {
            //        DesktopApi.SendToBackground(handle);
            //    }
            //    catch (Exception ex) {
            //        Debug.WriteLine(ex.Message);
            //    }
            //};
            
            window.Show();

            backgroundWindows.Add(monitorIndex, window);
            monitorIndex++;
        }

        LoadAndDistributeWidgets();
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

	        // This will automatically create layout.json in your active bin folder
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
}

