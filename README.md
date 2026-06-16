# Backdrop.NET

Backdrop.NET is a highly modular, extensible custom Windows shell built with C# and WPF. Designed to completely replace the native `explorer.exe`, Backdrop.NET hands absolute control of the desktop environment back to the user. 

Powered by [ManagedShell](https://github.com/cairoshell/managedshell), it handles low-level Windows APIs, task management, and system tray hooks, while providing a clean, decoupled canvas for you to build the exact desktop experience you want.

---

## ✨ Features
* **True Shell Replacement:** Runs as the lowest Z-order desktop layer, eliminating the need for hacky `Progman` or `WorkerW` injection.
* **Native WPF Widgets:** Build beautiful, hardware-accelerated desktop widgets using standard XAML.
* **Multi-Monitor Support:** Automatically detects physics bounds and scales widgets seamlessly across all connected displays.
* **JSON Layout Management:** Easily position and manage widget coordinates, Z-indexes, and assignments across different monitors via a simple `layout.json` file.

---

## 🧩 Extensibility & Plugin Architecture
Backdrop.NET is built around a strict **Dependency Injection & Plugin Architecture**. The core shell (`DesktopEngine`) is incredibly lightweight; everything else, from the taskbar to the clock, is an isolated `.dll` plugin.

### The Plugin Contract
Widget development is decoupled from the main application to prevent circular dependencies. To create a new widget, you simply reference the `Backdrop.NET.Contracts` library and implement the `IWidgetPlugin` interface:

```csharp
public interface IWidgetPlugin {
    string Name { get; } 
    string Description { get; }
    UIElement GetContent();
    void OnLoad();
    void OnUnload();
}
```

### Capability Interfaces (Interface Segregation)
Not all widgets need heavy system dependencies. Backdrop.NET uses "Capability Interfaces" to inject data only when requested.

For example, a simple digital clock only implements IWidgetPlugin. 
However, if you are building a custom taskbar or system tray, you can implement the IRequireShellManager interface. 
The PluginManager will detect this at runtime and securely inject the ManagedShell instance into your widget:
```csharp
public interface IRequireShellManager {
    void Initialize(ShellManager shellManager);
}
```

### Deployment
To install a new widget, simply drop its compiled .dll into the Widgets/ folder next to the DesktopEngine.exe. 
The engine dynamically loads it, maps its name to your layout.json, and handles the UI rendering autonomously.

## 🚀 Roadmap & Future Plans

Backdrop.NET is actively evolving from a foundational desktop overlay into a comprehensive, daily-driver Windows shell.

### Core Shell Components
 - System Tray (Notification Area): A dedicated plugin to handle background apps, volume, and network status utilizing ManagedShell's TrayService.
 - Start Menu & App Launcher: A fast launcher to index system paths, legacy Win32 executables, and UWP (Windows Store) apps.
 - Quick Settings (Action Center): A unified flyout for volume muting, network state, and power management (Shutdown/Restart) using ManagedShell helpers.

### Advanced OS Integrations
- Custom File Explorer: A standalone WPF file manager leveraging ManagedShell.ShellFolders to navigate directories, render native Windows file icons, and handle context menus natively without explorer.exe.
- Alt+Tab Task Switcher: A centralized, screen-dimming UI overlay to switch between active tasks visually.
- Universal Run Dialog: A Spotlight-style keyboard launcher triggered by Win + R or Win + S to execute quick commands or system searches.
- Virtual Desktop Support: Integration with the IVirtualDesktopManager COM interface to track and switch between Windows 11 virtual workspaces.
- Global Hotkey Manager: A background interceptor for native shell shortcuts (e.g., Win + Shift + S for Snip & Sketch).

### Custom Widget Ecosystem
- Media Controller: Hook into Windows Media Transport Controls (SMTC) for universal play/pause and skip functionalities.
- System Monitor: Hardware widgets tracking CPU, RAM, and GPU usage utilizing System.Diagnostics.PerformanceCounter.
- Weather & Clock Elements: Highly customizable aesthetic modules for the background canvas.
- Game Launchers: Aggregated local libraries (Steam, Epic, etc.) into a single, clean desktop grid.

### Engine Enhancements
- Design Mode (Drag & Drop): An interactive mode allowing users to click, drag, and resize widgets directly on the desktop canvas, automatically saving the new coordinates back to layout.json.
- Global Settings Service: An injected ISettingsManager allowing individual plugins to securely read/write their own configuration states.
- Inter-Plugin Communication: An Event Aggregator (Publish/Subscribe pattern) allowing standalone widgets to broadcast messages to each other without being tightly coupled.

## 🛠️ Getting Started

### ***(Note: Ensure you fully test your shell configuration in a virtual machine before replacing explorer.exe on your primary workstation and if that sounds scary to you, you don't need to be using this right now).***

#### Clone the repository and open Backdrop.NET.slnx.
    1. Ensure you have the .NET SDK installed.
    2. Build the DesktopEngine and Backdrop.NET.Contracts projects.
    3. Build your widget projects and ensure the compiled .dll files are copied to the Widgets/ output directory.
    4. Define your widget layout and screen coordinates in layout.json.
    5. Run DesktopEngine.exe.

Setting as the Default Shell
#### To run Backdrop.NET as your default Windows shell:
    1. Open the Registry Editor (regedit).
    2. Navigate to HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Winlogon.
    3. Add a new String Value (REG_SZ) named Shell.
    4. Set the value to the absolute path of your DesktopEngine.exe.
    5. Restart your computer or log out and log back in.