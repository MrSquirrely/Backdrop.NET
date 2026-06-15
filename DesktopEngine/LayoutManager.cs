using System.IO;
using System.Text.Json;
using Backdrop.NET.Models;

namespace Backdrop.NET;

public class LayoutManager {

	private readonly string layoutFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "layout.json");

    public void SaveLayout(IEnumerable<WidgetState> widgets) {
		JsonSerializerOptions options = new() {
			WriteIndented = true
		};

		string jsonString = JsonSerializer.Serialize(widgets, options);

		File.WriteAllText(layoutFilePath, jsonString);
	}

	public List<WidgetState> LoadLayout() {
		if (!File.Exists(layoutFilePath)) {
			return [];
		}

		string jsonString = File.ReadAllText(layoutFilePath);
		return JsonSerializer.Deserialize<List<WidgetState>>(jsonString) ?? [];
	}

}
