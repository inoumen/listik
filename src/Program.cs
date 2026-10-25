using System;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Language.Load();
        bool first;
        using (var mutex = new System.Threading.Mutex(true, "Local\\ListikDesktopNotes", out first))
        {
            if (!first)
                return;
            Application.Run(new NotesApp());
        }
    }
}
