using System.Runtime.InteropServices;

namespace QuickDock.Services;

public static class ShortcutResolver
{
    public static string? Resolve(string shortcutPath)
    {
        try
        {
            if (!System.IO.File.Exists(shortcutPath)) return null;

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return null;

            var shellInstance = Activator.CreateInstance(shellType);
            if (shellInstance == null) return null;

            var shortcut = shellType.InvokeMember("CreateShortcut",
                System.Reflection.BindingFlags.InvokeMethod,
                null, shellInstance, new object[] { shortcutPath });

            if (shortcut == null) return null;

            var targetPath = (string?)shellType.InvokeMember("TargetPath",
                System.Reflection.BindingFlags.GetProperty,
                null, shortcut, null);

            Marshal.ReleaseComObject(shortcut);
            Marshal.ReleaseComObject(shellInstance);

            return targetPath;
        }
        catch
        {
            return null;
        }
    }
}
