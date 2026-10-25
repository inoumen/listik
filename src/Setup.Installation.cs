using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;

partial class Setup
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    static extern bool MoveFileEx(string source, string destination, uint flags);
    static void Install()
    {
        foreach (var process in Process.GetProcessesByName("Listik"))
            using (process)
            {
                if (!process.HasExited)
                    throw new IOException("Listik is running.");
            }

        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Listik");
        Directory.CreateDirectory(dir);
        string exe = Path.Combine(dir, "Listik.exe");
        string staged = Path.Combine(dir, "Listik.installing");
        using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream("Listik.exe"))
            using (var output = File.Create(staged))
            {
                input.CopyTo(output);
                output.Flush(true);
            }

        for (int attempt = 0;; attempt++)
        {
            if (MoveFileEx(staged, exe, 9))
                break;
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            if (attempt == 5)
                throw new System.ComponentModel.Win32Exception(error);
            System.Threading.Thread.Sleep(100);
        }

        string link = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Listik.lnk");
        Type type = Type.GetTypeFromProgID("WScript.Shell");
        object shell = Activator.CreateInstance(type);
        object shortcut = type.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[]{link});
        shortcut.GetType().InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[]{exe});
        shortcut.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
    }
}
