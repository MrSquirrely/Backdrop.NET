using System.Diagnostics;
using System.IO;

namespace Backdrop.NET;

/// <summary>
/// Minimal file log at %LocalAppData%\Backdrop.NET\backdrop.log. Never throws.
/// </summary>
public static class Log {

    private static readonly Lock Gate = new();

    public static string LogFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Backdrop.NET", "backdrop.log");

    public static void Error(string message, Exception? exception = null) {
        string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR {message}{Environment.NewLine}"
                       + (exception is null ? string.Empty : $"{exception}{Environment.NewLine}");
        Debug.Write(entry);

        try {
            lock (Gate) {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
                File.AppendAllText(LogFilePath, entry);
            }
        }
        catch (Exception) {
            // Logging is best effort; a full disk or locked file must not take the shell down.
        }
    }

}