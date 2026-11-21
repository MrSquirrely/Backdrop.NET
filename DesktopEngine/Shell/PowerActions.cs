using System.Windows;

using ManagedShell.Common.Helpers;

namespace Backdrop.NET.Shell;

/// <summary>
/// The power options shared by the Start menu and Quick Settings. The ones that can lose unsaved work ask first.
/// </summary>
public static class PowerActions {

    public static bool CanSleep => PowerHelper.CanSleep();

    public static void Lock() {
        ShellHelper.Lock();
    }

    public static void Sleep() {
        PowerHelper.Sleep();
    }

    public static void SignOut() {
        if (Confirm("Sign out of Windows? Unsaved work in open apps may be lost.")) {
            ShellHelper.Logoff();
        }
    }

    public static void Restart() {
        if (Confirm("Restart the computer? Unsaved work in open apps may be lost.")) {
            PowerHelper.Reboot();
        }
    }

    public static void ShutDown() {
        if (Confirm("Shut down the computer? Unsaved work in open apps may be lost.")) {
            PowerHelper.Shutdown();
        }
    }

    private static bool Confirm(string question) {
        return iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(question, "Backdrop.NET", MessageBoxButton.YesNo,
                                                     MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
    }

}