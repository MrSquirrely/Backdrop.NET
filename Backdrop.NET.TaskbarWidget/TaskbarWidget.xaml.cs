using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using ManagedShell;
using ManagedShell.WindowsTasks;

namespace Backdrop.NET.TaskbarWidget;
/// <summary>
/// Interaction logic for TaskbarWidget.xaml
/// </summary>
public partial class TaskbarWidget : UserControl {
    private readonly ShellManager? shellManager;
    public ObservableCollection<ManualTask> FallbackTasks { get; } = [];

    public TaskbarWidget(ShellManager? manager) {
        InitializeComponent();
        shellManager = manager;
        Loaded += TaskbarWidget_Loaded;
    }

    private void TaskbarWidget_Loaded(object sender, RoutedEventArgs e) {
        // 1. Bind to ManagedShell's live collection whenever the tasks service exists. It is often
        //    empty at this point (at login, nothing is open yet) and fills in as windows appear.
        if (shellManager?.Tasks?.GroupedWindows is not null) {
            TaskItemsControl.ItemsSource = shellManager.Tasks.GroupedWindows;
            return;
        }

        // 2. No tasks service at all: fall back to a one-off snapshot of the open windows.
        LoadTasksManually();
        TaskItemsControl.ItemsSource = FallbackTasks;
    }

    private void LoadTasksManually() {
        FallbackTasks.Clear();

        NativeMethods.EnumWindows((hWnd, _) => {
            // Only grab windows that are currently visible
            if (NativeMethods.IsWindowVisible(hWnd)) {
                StringBuilder title = new(256);
                if (NativeMethods.GetWindowText(hWnd, title, 256) > 0) {
                    string appTitle = title.ToString();

                    // Filter out invisible overlays and our own background
                    if (!string.IsNullOrWhiteSpace(appTitle)
                        && (appTitle != "Program Manager")
                        && (appTitle != "Desktop Engine")) {
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
        Application.Current.Shutdown();
    }
}