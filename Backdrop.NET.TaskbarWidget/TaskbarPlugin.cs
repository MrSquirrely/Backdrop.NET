using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Backdrop.NET.Contracts;
using ManagedShell;

namespace Backdrop.NET.TaskbarWidget;

public class TaskbarPlugin : IWidgetPlugin, IRequireShellManager {

	public string Name => "Taskbar";
	public string Description => "The main system taskbar using ManagedShell.";

	private ShellManager? shellManager;

	// Receive the data from the Host
	public void Initialize(ShellManager manager) {
		shellManager = manager;
	}

	public UIElement GetContent() => new TaskbarWidget(shellManager!);

	public void OnLoad() { }

	public void OnUnload() { }

}
