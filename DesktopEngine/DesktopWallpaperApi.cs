using System.Runtime.InteropServices;

using WpfScreenHelper;

namespace Backdrop.NET;

public enum DesktopWallpaperPosition {
    DWPOS_CENTER = 0,
    DWPOS_TILE,
    DWPOS_STRETCH,
    DWPOS_FIT,
    DWPOS_FILL,
    DWPOS_SPAN,
}

[ComImport]
[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDesktopWallpaper {
    void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
    [return: MarshalAs(UnmanagedType.LPWStr)] string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);
    [return: MarshalAs(UnmanagedType.LPWStr)] string GetMonitorDevicePathAt(uint monitorIndex);
    uint GetMonitorDevicePathCount();
    DesktopApi.RECT GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);
    void SetBackgroundColor(uint color);
    uint GetBackgroundColor();
    void SetPosition([MarshalAs(UnmanagedType.I4)] DesktopWallpaperPosition position);
    [return: MarshalAs(UnmanagedType.I4)] DesktopWallpaperPosition GetPosition();
    void SetSlideshow(IntPtr items);
    IntPtr GetSlideshow();
    void SetSlideshowOptions(int options, uint slideshowTick);
    void GetSlideshowOptions(out int options, out uint slideshowTick);
    void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID, int direction);
    int GetStatus();
    void Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
}

public static class DesktopWallpaperFactory {
    [ComImport]
    [Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    private class DesktopWallpaperCoclass { }

    public static IDesktopWallpaper Create() {
        return (IDesktopWallpaper) new DesktopWallpaperCoclass();
    }
}

public static class DesktopWallpaperApi {
    private static readonly Lazy<IDesktopWallpaper> DesktopFactory = new(DesktopWallpaperFactory.Create);

    public static string? GetMonitorId(Screen? screen) {
        if (screen == null) return null;

        uint count = DesktopFactory.Value.GetMonitorDevicePathCount();
        for (uint i = 0; i < count; i++) {
            string monitorId = DesktopFactory.Value.GetMonitorDevicePathAt(i);
            DesktopApi.RECT rect = DesktopFactory.Value.GetMonitorRECT(monitorId);

            if (rect.Left == screen.Bounds.Left &&
                rect.Top == screen.Bounds.Top &&
                rect.Right == screen.Bounds.Right &&
                rect.Bottom == screen.Bounds.Bottom) {
                return monitorId;
            }
        }
        return null;
    }

    public static string GetWallpaper(string? monitorId) {
        if (monitorId == null) return string.Empty;
        return DesktopFactory.Value.GetWallpaper(monitorId);
    }

    public static DesktopWallpaperPosition GetPosition() {
        return DesktopFactory.Value.GetPosition();
    }
}