using System.Runtime.InteropServices;

namespace Backdrop.NET.Shell;

/// <summary>
/// Reads and sets the master volume and mute state of the default playback device (Windows Core Audio).
/// Every member returns false instead of throwing when there is no playback device.
/// </summary>
public static class AudioVolume {

    public static bool TryGet(out double level, out bool muted) {
        level = 0;
        muted = false;

        IAudioEndpointVolume? endpoint = GetEndpoint();
        if (endpoint is null) {
            return false;
        }

        try {
            if ((endpoint.GetMasterVolumeLevelScalar(out float scalar) != 0) || (endpoint.GetMute(out muted) != 0)) {
                return false;
            }

            level = scalar;
            return true;
        }
        finally {
            Marshal.ReleaseComObject(endpoint);
        }
    }

    /// <param name="level">0.0 (silent) to 1.0 (full).</param>
    public static bool SetLevel(double level) {
        IAudioEndpointVolume? endpoint = GetEndpoint();
        if (endpoint is null) {
            return false;
        }

        try {
            Guid context = Guid.Empty;
            return endpoint.SetMasterVolumeLevelScalar((float)Math.Clamp(level, 0.0, 1.0), ref context) == 0;
        }
        finally {
            Marshal.ReleaseComObject(endpoint);
        }
    }

    public static bool SetMuted(bool muted) {
        IAudioEndpointVolume? endpoint = GetEndpoint();
        if (endpoint is null) {
            return false;
        }

        try {
            Guid context = Guid.Empty;
            return endpoint.SetMute(muted, ref context) == 0;
        }
        finally {
            Marshal.ReleaseComObject(endpoint);
        }
    }

    private static IAudioEndpointVolume? GetEndpoint() {
        const int Render = 0;        // EDataFlow.eRender
        const int Multimedia = 1;    // ERole.eMultimedia
        const int ClsCtxAll = 0x17;

        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;

        try {
            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

            if ((enumerator.GetDefaultAudioEndpoint(Render, Multimedia, out device) != 0) || (device is null)) {
                return null;
            }

            Guid iid = typeof(IAudioEndpointVolume).GUID;
            return device.Activate(ref iid, ClsCtxAll, IntPtr.Zero, out object endpoint) == 0
                ? endpoint as IAudioEndpointVolume
                : null;
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException) {
            Log.Error("Could not reach the default audio device", ex);
            return null;
        }
        finally {
            if (device is not null) {
                Marshal.ReleaseComObject(device);
            }
            if (enumerator is not null) {
                Marshal.ReleaseComObject(enumerator);
            }
        }
    }

    // Only the members that are called are named; the rest are placeholders that keep the vtable order.

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator { }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice? endpoint);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice {
        [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int GetChannelCount(out uint channelCount);
        [PreserveSig] int SetMasterVolumeLevel(float levelDb, ref Guid eventContext);
        [PreserveSig] int SetMasterVolumeLevelScalar(float level, ref Guid eventContext);
        [PreserveSig] int GetMasterVolumeLevel(out float levelDb);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig] int SetChannelVolumeLevel(uint channel, float levelDb, ref Guid eventContext);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, ref Guid eventContext);
        [PreserveSig] int GetChannelVolumeLevel(uint channel, out float levelDb);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool muted, ref Guid eventContext);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);
    }

}