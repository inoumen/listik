using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

class SmokeTests
{
    static void Check(bool ok, string label)
    {
        if (!ok)
            throw new Exception(label);
        Console.WriteLine("PASS: " + label);
    }

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            Run(args);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    static void Run(string[] args)
    {
        string dir = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(dir);
        Application.EnableVisualStyles();
        var app = new NotesApp(dir, false);
        Check(app.Windows.Count == 1, "First launch creates a note");
        var note = app.Windows[0];
        Check(note.Data.Desktop && !note.Data.Top, "New notes default to desktop only");
        Check(note.FormBorderStyle == FormBorderStyle.None, "No native window frame");
        var editor = (RichTextBox)typeof(NoteWindow).GetField("editor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(note);
        editor.Text = "Hello · Bonjour · Привет · Привіт\n☐ Task";
        var dragArea = (Label)typeof(NoteWindow).GetField("dragArea", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(note);
        Check(dragArea.Text == "", "Note text is not duplicated in a title");
        editor.Select(0, 5);
        editor.SelectionFont = new Font("Segoe UI", 16, FontStyle.Bold);
        note.Data.Color = Color.Pink.ToArgb();
        note.Data.Opacity = .75;
        note.Data.Top = true;
        note.Data.Desktop = false;
        app.Save();
        string rtf = note.Data.Rtf;
        app.ExitThread();
        app = new NotesApp(dir, false);
        note = app.Windows[0];
        Check(note.Data.Rtf == rtf, "Unicode and rich text survive reload");
        Check(note.Data.Color == Color.Pink.ToArgb() && note.Data.Opacity == .75 && note.Data.Top, "Appearance survives reload");
        Check(!note.Data.Desktop, "Existing visibility preference survives reload");
        for (int i = 0; i < 20; i++)
            app.Save();
        Check(File.Exists(Path.Combine(dir, "notes.json")) && !File.Exists(Path.Combine(dir, "notes.json.tmp")), "Repeated atomic saves complete without leaving a temporary file");
        var options = typeof(NoteWindow).GetMethod("CreateOptionsMenu", BindingFlags.NonPublic | BindingFlags.Instance);
        var closingMenu = (ContextMenuStrip)options.Invoke(note, null);
        typeof(ToolStripDropDown).GetMethod("OnClosed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(closingMenu, new object[]{new ToolStripDropDownClosedEventArgs(ToolStripDropDownCloseReason.ItemClicked)});
        Check(!closingMenu.IsDisposed, "Menu survives Closed until WinForms finishes click handling");
        var nextMenu = (ContextMenuStrip)options.Invoke(note, null);
        Check(closingMenu.IsDisposed && !nextMenu.IsDisposed, "Old menu is released on next opening");
        note.Data.Top = false;
        note.ApplyVisibility(true, true);
        Check(note.Visible && !note.TopMost && !note.Data.Top, "Desktop notes remain below topmost shell flyouts");
        note.WindowState = FormWindowState.Minimized;
        note.ApplyVisibility(true, true);
        Check(!Native.IsIconic(note.Handle), "Minimized note restores without activation");
        note.ApplyVisibility(true, false);
        Check(note.Visible && !note.TopMost, "Opening another window leaves notes visible and coverable");
        note.ApplyVisibility(false, true);
        Check(!note.Visible, "Explicit hide takes priority over desktop visibility");
        foreach (string code in new[]{"en", "fr", "ru", "uk"})
        {
            Language.Current = code;
            var menu = (ContextMenuStrip)typeof(NoteWindow).GetMethod("CreateOptionsMenu", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(note, null);
            Check(menu.Items[0].Text == Language.T("Цвет стикера"), code + " menu translation");
            menu.Dispose();
        }

        app.Create();
        Check(app.Windows.Count == 2, "Create multiple notes");
        foreach (var w in app.Windows.ToArray())
            app.Delete(w);
        app.ExitThread();
        app = new NotesApp(dir, false);
        Check(app.Windows.Count == 0, "Permanent deletion survives reload without recreating notes");
        app.ExitThread();
    }
}
