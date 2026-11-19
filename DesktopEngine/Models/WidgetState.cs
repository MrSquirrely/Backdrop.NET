namespace Backdrop.NET.Models;

public class WidgetState {
    public Guid WidgetId { get; set; } = Guid.Empty;
    public string WidgetType { get; set; } = string.Empty;

    // Defaults apply when layout.json leaves a value out, so they must be usable on screen.
    public int MonitorIndex { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int ZIndex { get; set; }

}