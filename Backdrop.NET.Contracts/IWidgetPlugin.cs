using System;
using System.Windows;
using ManagedShell;

namespace Backdrop.NET.Contracts;

public interface IWidgetPlugin {
    string Name { get; }
    string Description { get; }
    UIElement GetContent();
    void OnLoad();
    void OnUnload();
}

public interface IRequireShellManager {
	void Initialize(ShellManager manager);
}
