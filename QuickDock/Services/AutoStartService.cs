using System.IO;
using System.Reflection;

namespace QuickDock.Services;

public class AutoStartService
{
    private const string AppName = "QuickDock";
    private readonly string _exePath;

    public AutoStartService()
    {
        _exePath = Environment.ProcessPath 
            ?? Assembly.GetExecutingAssembly().Location
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QuickDock.exe");
    }

    public bool IsEnabled()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
        return key?.GetValue(AppName) != null;
    }

    public void Enable()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        key?.SetValue(AppName, $"\"{_exePath}\"");
    }

    public void Disable()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        key?.DeleteValue(AppName, false);
    }
}
