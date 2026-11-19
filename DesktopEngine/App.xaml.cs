using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Backdrop.NET.Contracts;
using Backdrop.NET.Models;

using ManagedShell;

using WpfScreenHelper;

namespace Backdrop.NET;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App {

    private readonly Dictionary<int, BackgroundWindow> backgroundWindows = [];
    public static ShellManager ShellManager { get; private set; } = null!;
    public static PluginManager PluginManager { get; } = new();

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        ShellManager = new ShellManager();

        string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Widgets");
        PluginManager.LoadPlugins(pluginsPath);

        int monitorIndex = 0;

        foreach (Screen screen in Screen.AllScreens) {
            int physicsX = (int)screen.Bounds.Left;
            int physicsY = (int)screen.Bounds.Top;
            int physicsWidth = (int)screen.Bounds.Width;
            int physicsHeight = (int)screen.Bounds.Height;

            (double scaleX, double scaleY) = DpiHelper.GetMonitorScaleFactor(physicsX, physicsY);

            // Left/Top/Width/Height are only a starting hint; the window pins itself to the physical bounds.
            BackgroundWindow window = new(monitorIndex, physicsX, physicsY, physicsWidth, physicsHeight) {
                Left = physicsX / scaleX,
                Top = physicsY / scaleY,
                Width = physicsWidth / scaleX,
                Height = physicsHeight / scaleY,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize
            };

            window.WallpaperImage.Source = LoadWallpaper(screen, physicsWidth, physicsHeight);

            window.Show();
            backgroundWindows.Add(monitorIndex, window);
            monitorIndex++;
        }

        LoadAndDistributeWidgets();
    }

    // The shell is the user's whole desktop: log and keep running rather than leave a black screen.
    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
        Log.Error("Unhandled exception on the UI thread", e.Exception);
        e.Handled = true;
    }

    private static BitmapSource? LoadWallpaper(Screen screen, int targetWidth, int targetHeight) {
        try {
            string? monitorId = DesktopWallpaperApi.GetMonitorId(screen);
            string wallpaperPath = DesktopWallpaperApi.GetWallpaper(monitorId);

            if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath)) {
                return null;
            }

            using FileStream stream = new(wallpaperPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            // Read only the header to find out how far the image can be downscaled while still filling the screen
            BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            int sourceWidth = decoder.Frames[0].PixelWidth;
            int sourceHeight = decoder.Frames[0].PixelHeight;
            double scale = Math.Max((double)targetWidth / sourceWidth, (double)targetHeight / sourceHeight);

            stream.Position = 0;

            BitmapImage image = new();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad; // decode now so the file is not kept open
            image.StreamSource = stream;
            if (scale < 1.0) {
                image.DecodePixelWidth = (int)Math.Ceiling(sourceWidth * scale);
            }
            image.EndInit();
            image.Freeze();

            return image;
        }
        catch (Exception ex) {
            Log.Error($"Could not load the wallpaper for monitor {screen.DeviceName}", ex);
            return null;
        }
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
            bool monitorFound = true;

            if (!backgroundWindows.TryGetValue(savedWidget.MonitorIndex, out BackgroundWindow? targetWindow)) {
                monitorFound = false;

                if (!backgroundWindows.TryGetValue(0, out targetWindow)) {
                    continue;
                }
            }

            WidgetContainer container = CreateWidgetContainer(savedWidget);

            // A widget whose monitor is gone goes to the primary monitor's corner so it stays visible
            Canvas.SetLeft(container, monitorFound ? savedWidget.X : 0);
            Canvas.SetTop(container, monitorFound ? savedWidget.Y : 0);
            Panel.SetZIndex(container, savedWidget.ZIndex);

            targetWindow.WidgetCanvas.Children.Add(container);
        }
    }

    private static WidgetContainer CreateWidgetContainer(WidgetState state) {
        WidgetContainer container = new();

        try {
            // Dynamically ask the PluginManager to spawn the widget by name
            IWidgetPlugin? plugin = PluginManager.CreatePluginInstance(state.WidgetType, ShellManager);

            if (plugin is not null) {
                plugin.OnLoad(); // Fire the lifecycle event
                container.WidgetContentHost.Content = plugin.GetContent();
                return container;
            }
        }
        catch (Exception ex) {
            // A broken plugin must not take the shell down with it
            Log.Error($"Widget '{state.WidgetType}' failed to start", ex);
            container.WidgetContentHost.Content = CreatePlaceholder($"WIDGET '{state.WidgetType}' FAILED TO LOAD");
            return container;
        }

        container.WidgetContentHost.Content = CreatePlaceholder($"WIDGET '{state.WidgetType}' NOT FOUND");
        return container;
    }

    private static Border CreatePlaceholder(string text) {
        return new Border {
            Background = Brushes.Crimson,
            Width = 250,
            Height = 100,
            CornerRadius = new CornerRadius(10),
            Child = new TextBlock {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            }
        };
    }

    protected override void OnExit(ExitEventArgs e) {
        ShellManager.Dispose();
        base.OnExit(e);
    }
}