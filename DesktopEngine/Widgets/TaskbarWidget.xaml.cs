using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ManagedShell.WindowsTasks;

namespace Backdrop.NET.Widgets;
/// <summary>
/// Interaction logic for TaskbarWidget.xaml
/// </summary>
public partial class TaskbarWidget : UserControl {
	public ObservableCollection<ManualTask> FallbackTasks { get; set; } = [];

    public TaskbarWidget() {
        InitializeComponent();
        Loaded += TaskbarWidget_Loaded;
    }

    private void TaskbarWidget_Loaded(object sender, RoutedEventArgs e) {
	    // 1. Try ManagedShell directly using the correct GroupedWindows collection
	    if (App.ShellManager?.Tasks?.GroupedWindows != null && !App.ShellManager.Tasks.GroupedWindows.IsEmpty) {
		    TaskItemsControl.ItemsSource = App.ShellManager.Tasks.GroupedWindows;
		    return;
	    }

	    // 2. If ManagedShell is blocked by Windows Explorer, fallback to manual fetch!
	    LoadTasksManually();
	    TaskItemsControl.ItemsSource = FallbackTasks;
    }

    private void LoadTasksManually() {
	    FallbackTasks.Clear();

	    DesktopApi.EnumWindows((hWnd, lParam) => {
		    // Only grab windows that are currently visible
		    if (DesktopApi.IsWindowVisible(hWnd)) {
			    StringBuilder title = new StringBuilder(256);
			    if (DesktopApi.GetWindowText(hWnd, title, 256) > 0) {
				    string appTitle = title.ToString();

				    // Filter out invisible overlays and our own background
				    if (!string.IsNullOrWhiteSpace(appTitle) &&
				        appTitle != "Program Manager" &&
				        appTitle != "Desktop Engine") {
					    FallbackTasks.Add(new ManualTask {
						    Title = appTitle,
						    Handle = hWnd
					    });
				    }
			    }
		    }
		    return true; // Continue looping
	    }, IntPtr.Zero);
    }


    private void TaskButton_Click(object sender, RoutedEventArgs e) {
	    if (sender is Button btn) {
		    // If ManagedShell is working:
		    if (btn.DataContext is ApplicationWindow appWindow) {
			    appWindow.BringToFront();
		    }
		    // If our Manual Fallback is working:
		    else if (btn.DataContext is ManualTask manualTask) {
			    DesktopApi.SetForegroundWindow(manualTask.Handle);
		    }
	    }
    }

    public class ManualTask {
	    public string Title { get; set; } = string.Empty;
	    public IntPtr Handle { get; set; }
	    // Icon is intentionally left out for the fallback, it will just show the Title text.
    }
}
