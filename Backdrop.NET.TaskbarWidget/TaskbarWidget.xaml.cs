using ManagedShell.WindowsTasks;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ManagedShell;

namespace Backdrop.NET.TaskbarWidget;
/// <summary>
/// Interaction logic for TaskbarWidget.xaml
/// </summary>
public partial class TaskbarWidget : UserControl {
    private readonly ShellManager shellManager;
    public ObservableCollection<ManualTask> FallbackTasks { get; set; } = [];

    public TaskbarWidget(ShellManager manger) {
        InitializeComponent();
        shellManager = manger;
        Loaded += TaskbarWidget_Loaded;
    }

    private void TaskbarWidget_Loaded(object sender, RoutedEventArgs e) {
        // 1. Try ManagedShell directly using the correct GroupedWindows collection
        if (shellManager?.Tasks?.GroupedWindows != null && !shellManager.Tasks.GroupedWindows.IsEmpty) {
            TaskItemsControl.ItemsSource = shellManager.Tasks.GroupedWindows;
            return;
        }

        // 2. If ManagedShell is blocked by Windows Explorer, fallback to manual fetch!
        LoadTasksManually();
        TaskItemsControl.ItemsSource = FallbackTasks;
    }

    private void LoadTasksManually() {
        FallbackTasks.Clear();

        NativeMethods.EnumWindows((hWnd, lParam) => {
            // Only grab windows that are currently visible
            if (NativeMethods.IsWindowVisible(hWnd)) {
                StringBuilder title = new StringBuilder(256);
                if (NativeMethods.GetWindowText(hWnd, title, 256) > 0) {
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
	            NativeMethods.SetForegroundWindow(manualTask.Handle);
            }
        }
    }

    public class ManualTask {
        public string Title { get; set; } = string.Empty;
        public IntPtr Handle { get; set; }
        // Icon is intentionally left out for the fallback, it will just show the Title text.
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) {
	    System.Windows.Application.Current.Shutdown();
    }
}
