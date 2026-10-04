using System;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class StartupManager
{
    // Current user only. The option never changes another program's entry.
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string EntryName = "OnekoByMau.DesktopPet";
    static string Command { get { return "\"" + Application.ExecutablePath + "\""; } }

    public static bool IsEnabled()
    {
        using(RegistryKey key=Registry.CurrentUser.OpenSubKey(RunKey,false))
            return key!=null && !string.IsNullOrEmpty(key.GetValue(EntryName) as string);
    }

    public static void SetEnabled(bool enabled)
    {
        if(enabled) {
            using(RegistryKey key=Registry.CurrentUser.CreateSubKey(RunKey)) {
                if(key==null)throw new InvalidOperationException("No se pudo abrir la configuración de inicio de tu usuario.");
                key.SetValue(EntryName,Command,RegistryValueKind.String);
            }
        } else {
            using(RegistryKey key=Registry.CurrentUser.OpenSubKey(RunKey,true))
                if(key!=null)key.DeleteValue(EntryName,false);
        }
    }

    public static bool RefreshEnabledPath()
    {
        // When updating Oneko, retain the user's enabled choice and use this copy.
        // On first launch, no startup entry is created until the user enables it.
        using(RegistryKey key=Registry.CurrentUser.OpenSubKey(RunKey,false)) {
            string saved=key==null ? null:key.GetValue(EntryName) as string;
            if(string.IsNullOrEmpty(saved))return false;
            if(string.Equals(saved,Command,StringComparison.OrdinalIgnoreCase))return true;
        }
        SetEnabled(true);return true;
    }
}
