using System.IO;
using System.Text.Json;

using Backdrop.NET.Models;

namespace Backdrop.NET;

public class LayoutManager {

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        WriteIndented = true
    };

    private readonly string layoutFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "layout.json");

    public void SaveLayout(IEnumerable<WidgetState> widgets) {
        try {
            string jsonString = JsonSerializer.Serialize(widgets, SerializerOptions);
            File.WriteAllText(layoutFilePath, jsonString);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Error($"Could not save the layout to {layoutFilePath}", ex);
        }
    }

    public List<WidgetState> LoadLayout() {
        if (!File.Exists(layoutFilePath)) {
            return [];
        }

        try {
            string jsonString = File.ReadAllText(layoutFilePath);
            return JsonSerializer.Deserialize<List<WidgetState>>(jsonString) ?? [];
        }
        catch (JsonException ex) {
            Log.Error($"{layoutFilePath} is not valid; starting with the default layout", ex);
            BackUpBrokenLayout();
            return [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Error($"Could not read {layoutFilePath}", ex);
            return [];
        }
    }

    // The default layout is about to be written over the file, so keep the user's copy for them to repair.
    private void BackUpBrokenLayout() {
        try {
            File.Copy(layoutFilePath, layoutFilePath + ".bad", true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Error("Could not back up the invalid layout file", ex);
        }
    }

}