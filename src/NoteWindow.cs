using System;
using System.Drawing;
using System.Windows.Forms;

partial class NoteWindow : Form
{
    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84;
        if (m.Msg == WM_NCHITTEST)
        {
            long packed = m.LParam.ToInt64();
            Point p = PointToClient(new Point(unchecked((short)(packed & 0xffff)), unchecked((short)((packed >> 16) & 0xffff))));
            int edge = Padding.Left;
            bool left = p.X < edge, right = p.X >= ClientSize.Width - edge, top = p.Y < edge, bottom = p.Y >= ClientSize.Height - edge;
            int hit = top ? (left ? 13 : right ? 14 : 12) : bottom ? (left ? 16 : right ? 17 : 15) : left ? 10 : right ? 11 : 0;
            if (hit != 0)
            {
                m.Result = new IntPtr(hit);
                return;
            }
        }

        base.WndProc(ref m);
    }

    public NoteData Data;
    NotesApp app;
    RichTextBox editor;
    Panel toolbar;
    Label dragArea;
    bool loading = true;
    Point drag;
    bool dragging;
    ContextMenuStrip optionsMenu;
    protected override void Dispose(bool disposing)
    {
        if (disposing && optionsMenu != null)
        {
            optionsMenu.Dispose();
            optionsMenu = null;
        }

        base.Dispose(disposing);
    }

    public NoteWindow(NotesApp owner, NoteData data)
    {
        app = owner;
        Data = data;
        Text = "Listik";
        FormBorderStyle = FormBorderStyle.None;
        Padding = new Padding(5);
        ControlBox = false;
        ShowInTaskbar = false;
        MinimumSize = new Size(220, 80);
        StartPosition = FormStartPosition.Manual;
        Bounds = new Rectangle(data.X, data.Y, Math.Max(220, data.W), Math.Max(120, data.H));
        TopMost = data.Top;
        Opacity = Math.Max(.3, Math.Min(1, data.Opacity));
        toolbar = new Panel{Dock = DockStyle.Top, Height = 26};
        dragArea = new Label{Text = "", Dock = DockStyle.Fill};
        var add = CreateToolbarButton("+", () => app.Create());
        var more = CreateToolbarButton("···", () => CreateOptionsMenu().Show(toolbar, new Point(0, 34)));
        var delete = CreateToolbarButton("×", () => app.Delete(this));
        toolbar.Controls.Add(dragArea);
        toolbar.Controls.Add(more);
        toolbar.Controls.Add(add);
        toolbar.Controls.Add(delete);
        editor = new RichTextBox{Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 12), DetectUrls = true, AcceptsTab = true, HideSelection = false, ScrollBars = RichTextBoxScrollBars.Vertical, AllowDrop = true};
        Controls.Add(editor);
        Controls.Add(toolbar);
        if (!string.IsNullOrEmpty(data.Rtf))
            editor.Rtf = data.Rtf;
        editor.TextChanged += (s, e) =>
        {
            Data.Rtf = editor.Rtf;
            if (!loading)
                app.Changed();
        };
        editor.LinkClicked += (s, e) =>
        {
            Uri uri;
            if (Uri.TryCreate(e.LinkText, UriKind.Absolute, out uri) && (uri.Scheme == "http" || uri.Scheme == "https" || uri.Scheme == "mailto"))
                try
                {
                    System.Diagnostics.Process.Start(uri.AbsoluteUri);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
        };
        editor.DragEnter += (s, e) =>
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        };
        editor.DragDrop += (s, e) =>
        {
            foreach (string path in (string[])e.Data.GetData(DataFormats.FileDrop))
                InsertImage(path);
        };
        editor.MouseDown += (s, e) =>
        {
            if (e.Button != MouseButtons.Left)
                return;
            int i = editor.GetCharIndexFromPosition(e.Location);
            if (i < editor.TextLength && (editor.Text[i] == '☐' || editor.Text[i] == '☑'))
            {
                editor.Select(i, 1);
                editor.SelectedText = editor.Text[i] == '☐' ? "☑" : "☐";
            }
        };
        dragArea.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left && !Data.Locked)
            {
                drag = e.Location;
                dragging = true;
                dragArea.Capture = true;
            }
        };
        dragArea.MouseMove += (s, e) =>
        {
            if (dragging)
                Location = new Point(Left + e.X - drag.X, Top + e.Y - drag.Y);
        };
        dragArea.MouseUp += (s, e) =>
        {
            dragging = false;
            dragArea.Capture = false;
        };
        dragArea.DoubleClick += (s, e) => ToggleCollapsed();
        LocationChanged += (s, e) => CaptureBounds();
        ResizeEnd += (s, e) => CaptureBounds();
        FormClosing += (s, e) =>
        {
            e.Cancel = true;
            Hide();
        };
        ApplyNoteColor();
        EnsureVisible();
        if (data.Folded)
        {
            Height = 80;
            editor.Visible = false;
        }

        loading = false;
    }

    public void SyncContent()
    {
        Data.Rtf = editor.Rtf;
    }

    Button CreateToolbarButton(string text, Action action)
    {
        var b = new Button{Text = text, Dock = DockStyle.Right, Width = 32, FlatStyle = FlatStyle.Flat, TabStop = false};
        b.FlatAppearance.BorderSize = 0;
        b.Click += (s, e) => action();
        return b;
    }

    void CaptureBounds()
    {
        if (loading || WindowState != FormWindowState.Normal || Native.IsIconic(Handle))
            return;
        Data.X = Left;
        Data.Y = Top;
        Data.W = Width;
        if (!Data.Folded)
            Data.H = Height;
        app.Changed();
    }

    public void EnsureVisible()
    {
        var area = Screen.FromRectangle(Bounds).WorkingArea;
        Width = Math.Min(Width, area.Width);
        Height = Math.Min(Height, area.Height);
        Location = new Point(Math.Max(area.Left, Math.Min(Left, area.Right - Width)), Math.Max(area.Top, Math.Min(Top, area.Bottom - Height)));
        CaptureBounds();
    }

    public void ShowInactive()
    {
        Native.ShowWindow(Handle, Native.ShowWithoutActivation);
    }

    public void ApplyVisibility(bool show, bool desktop)
    {
        if (!show)
        {
            if (Visible)
                Hide();
            return;
        }

        // Show Desktop can minimize a window without changing its Visible property.
        // Restore without activation. Shell flyouts do not change note visibility.
        if (!Visible || Native.IsIconic(Handle))
            ShowInactive();
        bool onTop = Data.Top;
        if (TopMost != onTop)
            TopMost = onTop;
        // HWND_TOP raises above Explorer after Show Desktop without making notes
        // topmost: Start, tray flyouts and application windows can cover them.
        if (desktop)
            Native.SetWindowPos(Handle, Data.Top ? new IntPtr(-1) : IntPtr.Zero, 0, 0, 0, 0, Native.PreserveBoundsWithoutActivation);
    }

    void ApplyNoteColor()
    {
        var c = Color.FromArgb(Data.Color);
        BackColor = toolbar.BackColor = editor.BackColor = c;
        ForeColor = toolbar.ForeColor = editor.ForeColor = c.GetBrightness() < .45 ? Color.White : Color.FromArgb(40, 40, 40);
    }

    void ToggleCollapsed()
    {
        Data.Folded = !Data.Folded;
        editor.Visible = !Data.Folded;
        Height = Data.Folded ? 80 : Data.H;
        app.Changed();
    }

    void ToggleTextStyle(FontStyle style)
    {
        using (var font = new Font(editor.SelectionFont ?? editor.Font, (editor.SelectionFont ?? editor.Font).Style ^ style))
            editor.SelectionFont = font;
    }

    void InsertImage(string path)
    {
        try
        {
            using (var image = Image.FromFile(path))
            {
                Clipboard.SetImage(image);
                editor.Paste();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(Language.T("Не удалось вставить картинку:") + "\n" + ex.Message, "Listik");
        }
    }
}
