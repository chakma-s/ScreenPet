using Microsoft.Win32;

namespace ScreenPet;

/// <summary>
/// Manages Windows autostart via HKCU registry key.
/// Does NOT require administrator privileges.
/// </summary>
public sealed class StartupManager
{
    private const string RegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _name;
    private readonly string _exePath;

    public StartupManager(string name, string exePath)
    {
        _name    = name;
        _exePath = exePath;
    }

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath);
            return key?.GetValue(_name) is not null;
        }
    }

    public void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
        // Wrap path in quotes to handle spaces
        key?.SetValue(_name, $"\"{_exePath}\"");
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
        key?.DeleteValue(_name, throwOnMissingValue: false);
    }
}
