using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DesktopEngine;
/// <summary>
/// Interaction logic for WidgetContainer.xaml
/// </summary>
public partial class WidgetContainer : UserControl {

    private bool snapToGrid = true;
    private const double GridSize = 50.0;

    public WidgetContainer() {
        InitializeComponent();
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
	    base.OnPreviewMouseDown(e);

	    int highestZ = GetHighestZIndex();
        Canvas.SetZIndex(this, highestZ + 1);
    }

    private void DragGrip_OnDragDelta(object sender, DragDeltaEventArgs e) {
        double left = Canvas.GetLeft(this);
        double top = Canvas.GetTop(this);

        if (double.IsNaN(left)) left = 0;
        if (double.IsNaN(top)) top = 0;

        double rawX = left + e.HorizontalChange;
        double rawY = top + e.VerticalChange;

        if (snapToGrid) {
            rawX = Math.Round(rawX / GridSize) * GridSize;
            rawY = Math.Round(rawY / GridSize) * GridSize;
        }

        Canvas.SetLeft(this, rawX);
        Canvas.SetTop(this, rawY);
    }
}
