using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using WpfScreenHelper;

namespace DesktopEngine;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {

	private readonly Dictionary<int, MainWindow> backgroundWindows = new();

	protected override void OnStartup(StartupEventArgs e) {
		base.OnStartup(e);

		int monitorIndex = 0;

		foreach (Screen screen in Screen.AllScreens) {
			MainWindow window = new MainWindow(monitorIndex) {
				Left = screen.Bounds.Left,
				Top = screen.Bounds.Top,
				Width = screen.Bounds.Width,
				Height = screen.Bounds.Height,
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
}

