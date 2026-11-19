using System.IO;
using System.Reflection;

using Backdrop.NET.Contracts;

using ManagedShell;

namespace Backdrop.NET;

public class PluginManager {

    private readonly Dictionary<string, Type> availablePlugins = [];

    public void LoadPlugins(string pluginsDirectory) {
        string[] dllFiles;

        try {
            if (!Directory.Exists(pluginsDirectory)) {
                Directory.CreateDirectory(pluginsDirectory);
                return; //No plugins to load yet
            }

            dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Error($"Could not read the plugin folder {pluginsDirectory}", ex);
            return;
        }

        foreach (string dllFile in dllFiles) {
            try {
                Assembly assembly = Assembly.LoadFrom(dllFile);
                IEnumerable<Type> pluginTypes = assembly.GetTypes()
                                                        .Where(static t => typeof(IWidgetPlugin).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });
                foreach (Type pluginType in pluginTypes) {
                    if (Activator.CreateInstance(pluginType) is not IWidgetPlugin tempInstance) {
                        continue;
                    }

                    // First one wins: a later DLL must not silently replace an already registered widget
                    if (!availablePlugins.TryAdd(tempInstance.Name, pluginType)) {
                        Log.Error($"Plugin name '{tempInstance.Name}' in {dllFile} is already registered by "
                                  + $"{availablePlugins[tempInstance.Name].Assembly.Location}; skipping {pluginType.FullName}");
                    }
                }
            }
            catch (Exception ex) {
                Log.Error($"Failed to load plugin {dllFile}", ex);
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