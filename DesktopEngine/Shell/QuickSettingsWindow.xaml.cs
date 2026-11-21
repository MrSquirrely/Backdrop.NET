using System.Net.NetworkInformation;
using System.Windows;

using ManagedShell.Common.Helpers;

namespace Backdrop.NET.Shell;

/// <summary>
/// Quick Settings: volume and mute, network state, the auto-hide switch and power options.
/// Opens above the right end of the bar.
/// </summary>
public partial class QuickSettingsWindow {

    // True while the controls are being filled in from the system, so those changes are not written back
    private bool refreshing;

    public QuickSettingsWindow() {
        InitializeComponent();
        SleepButton.IsEnabled = PowerActions.CanSleep;
    }

    protected override FrameworkElement AnimatedRoot => PanelRoot;

    protected override bool AlignRight => true;

    protected override void OnOpening() {
        refreshing = true;

        try {
            bool hasAudio = AudioVolume.TryGet(out double level, out bool muted);
            VolumePanel.Visibility = hasAudio ? Visibility.Visible : Visibility.Collapsed;
            NoAudioText.Visibility = hasAudio ? Visibility.Collapsed : Visibility.Visible;

            if (hasAudio) {
                VolumeSlider.Value = Math.Round(level * 100);
                VolumeValue.Text = $"{VolumeSlider.Value:0}%";
                MuteToggle.IsOn = muted;
            }

            NetworkStatus.Text = DescribeNetwork();
            AutoHideToggle.IsOn = ShellSettings.Current.AutoHide;
        }
        finally {
            refreshing = false;
        }
    }

    private static string DescribeNetwork() {
        try {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return "Not connected";
            }

            // The adapter that actually routes traffic: up, not virtual plumbing, and has a gateway
            NetworkInterface? active = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(static nic =>
                (nic.OperationalStatus == OperationalStatus.Up)
                && (nic.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
                && (nic.GetIPProperties().GatewayAddresses.Count > 0));

            if (active is null) {
                return "Connected (local only)";
            }

            string kind = active.NetworkInterfaceType switch {
                NetworkInterfaceType.Wireless80211 => "Wi-Fi",
                NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet => "Ethernet",
                _ => active.NetworkInterfaceType.ToString()
            };

            return $"Connected: {kind} ({active.Name})";
        }
        catch (NetworkInformationException ex) {
            Log.Error("Could not read the network state", ex);
            return "Network state unavailable";
        }
    }

    #region Events

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        if (refreshing || (VolumeValue is null)) {
            return;
        }

        VolumeValue.Text = $"{e.NewValue:0}%";
        AudioVolume.SetLevel(e.NewValue / 100.0);
    }

    private void MuteToggle_Toggled(object sender, RoutedEventArgs e) {
        if (refreshing) {
            return;
        }

        AudioVolume.SetMuted(MuteToggle.IsOn);
    }

    private void AutoHideToggle_Toggled(object sender, RoutedEventArgs e) {
        if (refreshing) {
            return;
        }

        bool autoHide = AutoHideToggle.IsOn;
        ShellSettings.Update(s => s.AutoHide = autoHide);
    }

    private void AllSettings_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        SettingsWindow.ShowSingle();
    }

    private void NetworkSettings_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        ShellHelper.StartProcess("ms-settings:network");
    }

    private void Lock_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        PowerActions.Lock();
    }

    private void Sleep_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        PowerActions.Sleep();
    }

    private void Restart_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        PowerActions.Restart();
    }

    private void ShutDown_Click(object sender, RoutedEventArgs e) {
        CloseFlyout();
        PowerActions.ShutDown();
    }

    #endregion

}