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

        // 1. Calculate pure WPF logical movement delta
        double deltaX = mouseInWindow.X - lastMousePos.X;
        double deltaY = mouseInWindow.Y - lastMousePos.Y;

        logicalX += deltaX;
        logicalY += deltaY;
        lastMousePos = mouseInWindow;

        // 2. Find which monitor/window the mouse is currently hovering over
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

        // 3. Hot-Swap monitors if the mouse crossed a screen boundary
        if ((targetWindow != currentWindow) && Parent is Canvas currentCanvas) {
            currentCanvas.Children.Remove(this);
            targetWindow.WidgetCanvas.Children.Add(this);

            // Translate the widget's logical coordinates into the new window's coordinate space
            Point widgetPhysical = currentWindow.PointToScreen(new Point(logicalX, logicalY));
            Point newLogicalPos = targetWindow.PointFromScreen(widgetPhysical);

            logicalX = newLogicalPos.X;
            logicalY = newLogicalPos.Y;

            // Reset the mouse tracker relative to the new window
            lastMousePos = targetWindow.PointFromScreen(physicalMouse);

            ((FrameworkElement)sender).CaptureMouse();
        }

        // 4. Snap to grid visually, but keep the internal tracking exact
        double renderX = logicalX;
        double renderY = logicalY;

        if (snapToGrid) {
            renderX = Math.Round(renderX / GridSize) * GridSize;
            renderY = Math.Round(renderY / GridSize) * GridSize;
        }

        Canvas.SetLeft(this, renderX);
        Canvas.SetTop(this, renderY);
    }

    // Keep this empty so XAML compiler doesn't complain if DragDelta is still bound in the markup
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
//private readonly bool snapToGrid = true;
//private const double GridSize = 50.0;

//public WidgetContainer() {
//    InitializeComponent();
//}

//protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
// base.OnPreviewMouseDown(e);

// int highestZ = GetHighestZIndex();
//    Canvas.SetZIndex(this, highestZ + 1);
//}

//private int GetHighestZIndex() {
// if (Parent is Panel parentPanel) {
//  return parentPanel.Children.OfType<UIElement>().Select(Canvas.GetZIndex).DefaultIfEmpty(0).Max();
// }

// return 0;
//}


//private void DragGrip_OnDragDelta(object sender, DragDeltaEventArgs e) {
//    double left = Canvas.GetLeft(this);
//    double top = Canvas.GetTop(this);

//    if (double.IsNaN(left)) left = 0;
//    if (double.IsNaN(top)) top = 0;

//    double rawX = left + e.HorizontalChange;
//    double rawY = top + e.VerticalChange;

//    if (snapToGrid) {
//        rawX = Math.Round(rawX / GridSize) * GridSize;
//        rawY = Math.Round(rawY / GridSize) * GridSize;
//    }

//    Canvas.SetLeft(this, rawX);
//    Canvas.SetTop(this, rawY);
//}