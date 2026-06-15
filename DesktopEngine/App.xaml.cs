using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

using DesktopEngine.Models;

using WpfScreenHelper;

namespace DesktopEngine;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {

    private readonly Dictionary<int, BackgroundWindow> backgroundWindows = new();

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

            window.Show();

            IntPtr handle = new WindowInteropHelper(window).Handle;
            DesktopApi.SendToBackground(handle);

            backgroundWindows.Add(monitorIndex, window);
            monitorIndex++;
        }
    }

    private void LoadAndDistributeWidgets() {
        LayoutManager manager = new();
        List<WidgetState> savedWidgets = manager.LoadLayout();

        foreach (WidgetState savedWidget in savedWidgets) {
            if (backgroundWindows.TryGetValue(savedWidget.MonitorIndex, out BackgroundWindow targetWindow)) {
                WidgetContainer container = CreateWidgetContainer(savedWidget);

                Canvas.SetLeft(container, savedWidget.X);
                Canvas.SetTop(container, savedWidget.Y);
                Panel.SetZIndex(container, savedWidget.ZIndex);

                targetWindow.WidgetCanvas.Children.Add(container);
            }
            else {
                if (!backgroundWindows.TryGetValue(0, out BackgroundWindow primaryWindow)) {
                    continue;
                }

                WidgetContainer container = CreateWidgetContainer(savedWidget);
                Canvas.SetLeft(container, 0); // Reset X to ensure it's visible
                Canvas.SetTop(container, 0);  // Reset Y to ensure it's visible
                primaryWindow.WidgetCanvas.Children.Add(container);
            }
        }
    }

    private WidgetContainer CreateWidgetContainer(WidgetState state) {
        WidgetContainer container = new();

        //todo: add some default states
        switch (state.WidgetType) {
            default:
                container.WidgetContentHost.Content = new TextBlock {
                    Text = "Unknown Widget"
                };
                break;
        }

        return container;
    }
}

