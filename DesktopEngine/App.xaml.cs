using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Backdrop.NET.Contracts;
using Backdrop.NET.Models;
using Backdrop.NET.Widgets;
using ManagedShell;
using WpfScreenHelper;

namespace Backdrop.NET;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App {

    private readonly Dictionary<int, BackgroundWindow?> backgroundWindows = new();
    public static ShellManager ShellManager { get; private set; } = null!;
    public static PluginManager PluginManager { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e) {
	    base.OnStartup(e);

	    ShellManager = new ShellManager();

	    string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Widgets");
	    PluginManager.LoadPlugins(pluginsPath);

        int monitorIndex = 0;

	    foreach (Screen screen in Screen.AllScreens) {
		    (double scaleX, double scaleY) = DpiHelper.GetMonitorScaleFactor(screen.Bounds.Left, screen.Bounds.Top);

		    int physicsX = (int)screen.Bounds.Left;
		    int physicsY = (int)screen.Bounds.Top;
		    int physicsWidth = (int)screen.Bounds.Width;
		    int physicsHeight = (int)screen.Bounds.Height;

			string? monitorId = DesktopWallpaperApi.GetMonitorId(screen);
			string wallpaperPath = DesktopWallpaperApi.GetWallpaper(monitorId);

			BackgroundWindow window = new(monitorIndex, physicsX, physicsY, physicsWidth, physicsHeight) {
			    Left = physicsX / scaleX,
			    Top = physicsY / scaleY,
			    Width = physicsWidth / scaleX,
			    Height = physicsHeight / scaleY,
			    WindowStyle = WindowStyle.None,
			    ResizeMode = ResizeMode.NoResize
		    };

			if (!string.IsNullOrEmpty(wallpaperPath) && File.Exists(wallpaperPath)) {
				window.WallpaperImage.Source = new BitmapImage(new Uri(wallpaperPath));
			}

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

	    // Dynamically ask the PluginManager to spawn the widget by name
	    IWidgetPlugin? plugin = PluginManager.CreatePluginInstance(state.WidgetType, ShellManager);

	    if (plugin != null) {
		    plugin.OnLoad(); // Fire the lifecycle event
		    container.WidgetContentHost.Content = plugin.GetContent();
	    }
	    else {
		    // Fallback for missing or broken plugins
		    container.WidgetContentHost.Content = new Border {
			    Background = Brushes.Crimson,
			    Width = 250,
			    Height = 100,
			    CornerRadius = new CornerRadius(10),
			    Child = new TextBlock {
				    Text = $"WIDGET '{state.WidgetType}' NOT FOUND",
				    Foreground = Brushes.White,
				    FontSize = 16,
				    FontWeight = FontWeights.Bold,
				    HorizontalAlignment = HorizontalAlignment.Center,
				    VerticalAlignment = VerticalAlignment.Center,
				    TextWrapping = TextWrapping.Wrap
			    }
		    };
	    }

	    return container;
    }

    // private WidgetContainer CreateWidgetContainer(WidgetState state) {
    //     WidgetContainer container = new();

    //     //todo: add some default states
    //     switch (state.WidgetType) {
    //case "Taskbar":
    //	container.WidgetContentHost.Content = new TaskbarWidget();
    //             break;
    //         case "Clock":
    //             container.WidgetContentHost.Content = new ClockWidget();
    //             break;
    //         default:
    //          container.WidgetContentHost.Content = new Border {
    //           Background = Brushes.Crimson,
    //           Width = 250,
    //           Height = 100,
    //           CornerRadius = new CornerRadius(10),
    //           Child = new TextBlock {
    //            Text = "I AM A WIDGET",
    //            Foreground = Brushes.White,
    //            FontSize = 24,
    //            FontWeight = FontWeights.Bold,
    //            HorizontalAlignment = HorizontalAlignment.Center,
    //            VerticalAlignment = VerticalAlignment.Center
    //           }
    //          };
    //             break;
    //     }

    //     return container;
    // }

    protected override void OnExit(ExitEventArgs e) {
	    ShellManager.Dispose();
        base.OnExit(e);
    }
}

