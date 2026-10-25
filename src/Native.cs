using System;
using System.Runtime.InteropServices;

static class Native
{
    public const int ShowWithoutActivation = 4;
    public const uint PreserveBoundsWithoutActivation = 0x0013;
    const uint ReplaceExistingAndFlush = 0x0009;
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool MoveFileEx(string source, string destination, uint flags);
    public static void CommitFile(string source, string destination)
    {
        // File scanners can briefly hold the destination without delete sharing.
        for (int attempt = 0;; attempt++)
        {
            if (MoveFileEx(source, destination, ReplaceExistingAndFlush))
                return;
            int error = Marshal.GetLastWin32Error();
            if (attempt >= 5 || (error != 5 && error != 32 && error != 33))
                throw new System.ComponentModel.Win32Exception(error);
            System.Threading.Thread.Sleep(50 * (attempt + 1));
        }
    }

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassName(IntPtr hwnd, System.Text.StringBuilder text, int count);
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    public static bool IsDesktop(IntPtr hwnd)
    {
        var name = new System.Text.StringBuilder(256);
        GetClassName(hwnd, name, 256);
        return name.ToString() == "Progman" || name.ToString() == "WorkerW";
    }
}
