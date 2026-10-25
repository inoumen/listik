using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;

partial class NotesApp : ApplicationContext
{
    public List<NoteWindow> Windows = new List<NoteWindow>();
    NotifyIcon tray;
    Timer timer;
    bool hidden, exiting, dirty;
    NoteStore store;
    bool showWindows;
    public NotesApp(string dataDirectory = null, bool displayWindows = true)
    {
        showWindows = displayWindows;
        store = new NoteStore(dataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Listik"));
        InitializeTray();
        if (store.Exists)
        {
            try
            {
                foreach (var data in store.Load())
                    Add(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Language.T("Не удалось прочитать заметки. Исходные данные сохранены.") + "\n" + ex.Message, "Listik");
                tray.Dispose();
                Environment.Exit(1);
            }
        }
        else
            Create();
        timer = new Timer{Interval = 600};
        timer.Tick += (s, e) =>
        {
            if (dirty)
                Save();
            RefreshVisibility();
        };
        timer.Start();
        if (showWindows)
            SystemEvents.DisplaySettingsChanged += DisplayChanged;
    }

    void Add(NoteData data)
    {
        var window = new NoteWindow(this, data);
        Windows.Add(window);
        if (showWindows)
            window.Show();
    }

    public void Create()
    {
        hidden = false;
        Add(new NoteData{X = Screen.PrimaryScreen.WorkingArea.X + 80 + Windows.Count % 8 * 26, Y = Screen.PrimaryScreen.WorkingArea.Y + 80 + Windows.Count % 8 * 26});
        Changed();
    }

    public void Changed()
    {
        dirty = true;
    }

    public void Delete(NoteWindow window)
    {
        Windows.Remove(window);
        window.Hide();
        Changed();
        Save();
        if (window.IsHandleCreated)
            window.BeginInvoke((Action)(() => window.Dispose()));
        else
            window.Dispose();
    }

    public void Save()
    {
        try
        {
            foreach (var window in Windows)
                window.SyncContent();
            store.Save(Windows.Select(window => window.Data).ToList());
            dirty = false;
        }
        catch (Exception ex)
        {
            if (!showWindows)
                throw;
            if (timer != null)
                timer.Stop();
            MessageBox.Show(Language.T("Не удалось сохранить заметки:") + "\n" + ex.Message, "Listik");
            if (timer != null)
                timer.Start();
        }
    }

    void DisplayChanged(object s, EventArgs e)
    {
        foreach (var w in Windows.ToArray())
            if (!w.IsDisposed)
                w.BeginInvoke((Action)(() => w.EnsureVisible()));
    }

    void RefreshVisibility()
    {
        if (!showWindows)
            return;
        IntPtr fg = Native.GetForegroundWindow();
        bool desktop = Native.IsDesktop(fg);
        foreach (var w in Windows)
            w.ApplyVisibility(!hidden, desktop);
    }

    protected override void ExitThreadCore()
    {
        if (exiting)
            return;
        exiting = true;
        Save();
        timer.Stop();
        SystemEvents.DisplaySettingsChanged -= DisplayChanged;
        foreach (var w in Windows.ToArray())
            w.Dispose();
        tray.Dispose();
        base.ExitThreadCore();
    }
}
