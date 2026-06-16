using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Backdrop.NET;
/// <summary>
/// Interaction logic for WidgetContainer.xaml
/// </summary>
public partial class WidgetContainer {
    private bool snapToGrid = true;
    private const double GridSize = 50.0;

    private bool isDragging;

    // We track the EXACT mouse math separately from the visual snapped position
    private double logicalX;
    private double logicalY;
    private Point lastMousePos;

    public WidgetContainer() {
        InitializeComponent();
        Loaded += OnWidgetLoaded;
    }

    private void OnWidgetLoaded(object sender, RoutedEventArgs e) {
	    if (FindName("DragGrip") is not FrameworkElement grip) {
		    return;
	    }

	    // Prevent duplicate event binding if the widget is moved to a new screen
	    grip.PreviewMouseLeftButtonDown -= Grip_MouseDown;
	    grip.PreviewMouseMove -= Grip_MouseMove;
	    grip.PreviewMouseLeftButtonUp -= Grip_MouseUp;

	    grip.PreviewMouseLeftButtonDown += Grip_MouseDown;
	    grip.PreviewMouseMove += Grip_MouseMove;
	    grip.PreviewMouseLeftButtonUp += Grip_MouseUp;
    }

    private void Grip_MouseDown(object sender, MouseButtonEventArgs e) {
        isDragging = true;

        logicalX = Canvas.GetLeft(this);
        logicalY = Canvas.GetTop(this);
        if (double.IsNaN(logicalX)) logicalX = 0;
        if (double.IsNaN(logicalY)) logicalY = 0;

        if (Window.GetWindow(this) is { } currentWindow) {
            lastMousePos = e.GetPosition(currentWindow);
        }

        ((FrameworkElement)sender).CaptureMouse();
        e.Handled = true;
    }

    private void Grip_MouseUp(object sender, MouseButtonEventArgs e) {
        isDragging = false;
        ((FrameworkElement)sender).ReleaseMouseCapture();
        e.Handled = true;
    }

    private void Grip_MouseMove(object sender, MouseEventArgs e) {
        if (!isDragging) return;

        if (Window.GetWindow(this) is not BackgroundWindow currentWindow) return;

        Point mouseInWindow = e.GetPosition(currentWindow);

        double deltaX = mouseInWindow.X - lastMousePos.X;
        double deltaY = mouseInWindow.Y - lastMousePos.Y;

        logicalX += deltaX;
        logicalY += deltaY;
        lastMousePos = mouseInWindow;

        Point physicalMouse = currentWindow.PointToScreen(mouseInWindow);
        BackgroundWindow targetWindow = currentWindow;

        foreach (BackgroundWindow win in Application.Current.Windows.OfType<BackgroundWindow>()) {
            Point localTest = win.PointFromScreen(physicalMouse);

            // If the mouse is inside this window's bounds, we found our target monitor
            if (!(localTest.X >= 0) || !(localTest.X <= win.Width) ||
                !(localTest.Y >= 0) || !(localTest.Y <= win.Height)) {
	            continue;
            }

            targetWindow = win;
            break;
        }

        if ((targetWindow != currentWindow) && Parent is Canvas currentCanvas) {
            currentCanvas.Children.Remove(this);
            targetWindow.WidgetCanvas.Children.Add(this);

            Point widgetPhysical = currentWindow.PointToScreen(new Point(logicalX, logicalY));
            Point newLogicalPos = targetWindow.PointFromScreen(widgetPhysical);

            logicalX = newLogicalPos.X;
            logicalY = newLogicalPos.Y;

            lastMousePos = targetWindow.PointFromScreen(physicalMouse);

            ((FrameworkElement)sender).CaptureMouse();
        }

        double renderX = logicalX;
        double renderY = logicalY;

        if (snapToGrid) {
            renderX = Math.Round(renderX / GridSize) * GridSize;
            renderY = Math.Round(renderY / GridSize) * GridSize;
        }

        Canvas.SetLeft(this, renderX);
        Canvas.SetTop(this, renderY);
    }

    private void DragGrip_OnDragDelta(object sender, DragDeltaEventArgs e) { }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
        base.OnPreviewMouseDown(e);

        int highestZ = GetHighestZIndex();
        Panel.SetZIndex(this, highestZ + 1);
    }

    private int GetHighestZIndex() {
        if (Parent is Panel parentPanel) {
            return parentPanel.Children.OfType<UIElement>()
                                       .Select(Panel.GetZIndex)
                                       .DefaultIfEmpty(0)
                                       .Max();
        }
        return 0;
    }
}