using Backdrop.NET.Contracts;

using ManagedShell;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Backdrop.NET;

public class PluginManager {

	private readonly Dictionary<string, Type> availablePlugins = new();

	public void LoadPlugins(string pluginsDirectory) {
		if (!Directory.Exists(pluginsDirectory)) {
			Directory.CreateDirectory(pluginsDirectory);
			return; //No plugins to load yet
		}

		string[] dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll");

		foreach (string dllFile in dllFiles) {
			try {
				Assembly assembly = Assembly.LoadFrom(dllFile);
				IEnumerable<Type> pluginTypes = assembly.GetTypes()
				                                        .Where(t => typeof(IWidgetPlugin).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });
				foreach (Type pluginType in pluginTypes) {
					if (Activator.CreateInstance(pluginType) is IWidgetPlugin tempInstance) {
						availablePlugins[tempInstance.Name] = pluginType;
					}
				}
			}
			catch (Exception ex) {
				// Log the error (you can use ShellLogger from ManagedShell here)
				Console.WriteLine($"Failed to load plugin {dllFile}: {ex.Message}");
			}
        }
	}

	public IWidgetPlugin? CreatePluginInstance(string widgetType, ShellManager shellManager) {
		if (availablePlugins.TryGetValue(widgetType, out Type? type)) {
			if (Activator.CreateInstance(type) is IWidgetPlugin plugin) {

				// Capability Check: Does this specific plugin want the ShellManager?
				if (plugin is IRequireShellManager shellAwarePlugin) {
					shellAwarePlugin.Initialize(shellManager);
				}

				// (Future proofing) If you add an IRequireNetwork later, you'd just add another check here:
				// if (plugin is IRequireNetwork netPlugin) { netPlugin.Initialize(NetworkService); }

				return plugin;
			}
		}
		return null;
    }

}
