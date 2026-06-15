namespace Backdrop.NET.Models;

public class WidgetState {

	public Guid WidgetId { get; set; } = Guid.Empty;
	public string WidgetType { get; set; } = string.Empty;
	public int MonitorIndex { get; set; } = int.MinValue;
	public double X { get; set; } = double.MinValue;
	public double Y { get; set; } = double.MinValue;
	public int ZIndex { get; set; } = int.MinValue;

}
