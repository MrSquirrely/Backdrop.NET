# Backdrop.NET - Custom Shell & Widget Roadmap

## 1. Core Shell Components
*Replacing the essential features of explorer.exe utilizing ManagedShell data.*

- [ ] **System Tray (Notification Area)**
  - Bind to `ShellManager.Tray.NotifyIcons`.
  - Required to view and interact with background apps (Discord, antivirus, cloud sync tools).
- [ ] **Start Menu / App Launcher**
  - Parse `%ProgramData%\Microsoft\Windows\Start Menu\Programs`.
  - Fetch installed UWP (Windows Store) apps using ManagedShell helpers.
  - Implement as a full-screen overlay or traditional pop-up menu.
- [ ] **Quick Settings (Action Center)**
  - Utilize `VolumeHelper` and `PowerHelper`.
  - Add controls for volume muting/adjustment, network state, and power management (shutdown/restart).

## 2. Standard Productivity Widgets
*Standalone `.dll` projects built on the decoupled plugin architecture.*

- [ ] **Media Controller**
  - Hook into Windows Media Transport Controls (SMTC).
  - Display currently playing media with active play/pause and skip buttons.
- [ ] **System Monitor**
  - Track CPU, RAM, and GPU usage using `System.Diagnostics.PerformanceCounter`.
- [ ] **Weather & Clock**
  - Highly customizable digital/analog clock UI.
  - Weather fetcher utilizing a free API (e.g., OpenWeatherMap).

## 3. Custom & Specialized Widgets
*Tailored desktop utilities designed for specific personal workflows and hobbies.*

- [ ] **Unified Game Launcher**
  - Parse local game libraries.
  - Aggregate titles from Steam and Humble Choice into a single, clean desktop grid for one-click launching.
- [ ] **Server Ping/Uptime Monitor**
  - Continuously track the health and ping of domains or virtual private servers hosted on Hostinger.
  - Visual status indicators (e.g., UI changes color if a server goes down).
- [ ] **High Lexicon Scratchpad**
  - Specialized text-entry widget for fictional linguistics and translation.
  - Strict keyboard input restriction to only the 12 permitted letters (A, E, G, I, K, O, P, R, S, T, U, V).
- [ ] **Chaotic Visual Scripter**
  - Node-based desktop UI to dynamically chain C# extension methods together.
  - Route data through absurd logical loops to execute simple tasks (like changing a wallpaper or moving a widget).

## 4. Architectural Additions
*Foundational systems required for the `DesktopEngine` host to support a growing plugin ecosystem.*

- [ ] **Global Settings Service**
  - Create an `ISettingsManager` in the Contracts library.
  - Allow plugins to request the manager to easily read/write their own JSON configuration states.
- [ ] **Widget Drag-and-Drop (Design Mode)**
  - Allow repositioning of widgets dynamically on the desktop canvas.
  - Automatically calculate and save new X/Y coordinates to `layout.json`.
- [ ] **Z-Index Management**
  - Context menu options (Right-Click -> "Send to Back" / "Bring to Front") to manage overlapping widgets elegantly.
- [ ] **Inter-Plugin Communication**
  - Implement an Event Aggregator (Publish/Subscribe pattern) in the Contracts library.
  - Enable widgets to broadcast messages without tight coupling (e.g., the Game Launcher broadcasting an event that tells the Media Controller to pause).

## 5. Advanced Core Shell Systems
*Crucial OS integrations that `explorer.exe` normally handles silently.*

- [ ] **Desktop Icon & File Manager (`ShellFolders`)**
  - **The Problem:** Without Explorer, you have no desktop icons, no recycle bin, and no ability to right-click the desktop to create a new text file.
  - **The Solution:** Utilize ManagedShell's `ManagedShell.ShellFolders` namespace. It provides `ShellFolder` and `ShellItem` classes that can read the exact contents of the user's Desktop folder, render the standard Windows file icons, and even hook into the native Windows right-click context menus (Open, Copy, Properties) using the `ShellFolderContextMenu` class.

- [ ] **Global Hotkey Manager (The Win-Key Interceptor)**
  - **The Problem:** Standard Windows shortcuts like `Win + R` (Run), `Win + E` (File Explorer), or `Win + S` (Search) are handled by Explorer. If Explorer is dead, pressing those keys does nothing.
  - **The Solution:** Build a hidden background service using the Win32 `RegisterHotKey` API. This allows your shell to listen for those specific key presses globally and trigger your own custom widgets (e.g., popping open your custom Run dialog or Start Menu).

- [ ] **Custom Task Switcher (Alt + Tab Replacement)**
  - **The Problem:** While the basic Windows Alt+Tab *might* still function under certain conditions without Explorer, it often becomes unstable or ugly without the DWM (Desktop Window Manager) integrations Explorer provides. 
  - **The Solution:** Since you already have the `App.ShellManager.Tasks.GroupedWindows` list for your taskbar, you can create a centralized, screen-dimming overlay widget that spawns when the user presses `Alt + Tab`, allowing them to visually select the active window.

- [ ] **The "Run" Dialog / Universal Search**
  - **The Problem:** Power users rely heavily on pressing `Win + R` to type commands or executing quick math/web queries.
  - **The Solution:** Create a widget similar to macOS "Spotlight" or "PowerToys Run". You can use ManagedShell's `SearchHelper` class to index the system, allowing the user to press a hotkey, type an app name or file path, and instantly launch it.

- [ ] **Virtual Desktop Integration (Task View)**
  - **The Problem:** Windows 10 and 11 feature Virtual Desktops, but the UI to switch between them is deeply embedded in the Explorer taskbar.
  - **The Solution:** Implement the `IVirtualDesktopManager` COM interface. This allows your custom Taskbar widget to show which virtual desktop the user is on, filter the open windows so only the apps on the *current* desktop show up in the taskbar, and provide buttons to switch between desktops.

- [ ] **Screen Clipping / Snip & Sketch Hook**
  - **The Problem:** The `PrintScreen` key and `Win + Shift + S` rely heavily on the shell environment.
  - **The Solution:** You can map a global hotkey to directly launch the modern Windows snipping tool executable (`ms-screenclip:` URI protocol) so you don't lose the ability to take quick area screenshots.

- [ ] **Custom File Explorer Application**
  - **The Problem:** Without `explorer.exe`, users lose the native Windows file browsing experience (the `Win + E` window). While you can still use the command line or third-party tools, a true custom shell needs its own cohesive file manager.
  - **The Solution:** Build a standalone WPF application or full-screen widget that acts as the primary file browser.
  - **Implementation Details:**
    - Use `ManagedShell.ShellFolders.ShellFolder` to navigate directories and query contents natively.
    - Bind to `ShellItem` collections to populate your UI (List, Grid, or Column views).
    - Utilize `ManagedShell.Common.Helpers.IconImageConverter` to extract and display the correct Windows icons for every file type and folder.
    - Integrate `ShellItemContextMenu` and `ShellFolderContextMenu` so when a user right-clicks a file in your custom UI, they get the exact native Windows context menu (Open, Copy, Properties, "Open with Code", etc.) without you having to code those actions manually.