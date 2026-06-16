using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Backdrop.NET.Contracts;
using ManagedShell;

namespace Backdrop.NET.ClockWidget;

public class ClockPlugin : IWidgetPlugin {
	public string Name => "Clock";
	public string Description => "Just a standard clock cause it was the easiest thing to to.";
	public UIElement GetContent() => new ClockWidget();

	public void OnLoad() {

	}
	public void OnUnload() {

	}
}
