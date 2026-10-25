using System;
using System.Drawing;
using System.Windows.Forms;

partial class NoteWindow
{
    ToolStripMenuItem CreateToggleItem(string name, Action action, bool check)
    {
        var item = new ToolStripMenuItem(name)
        {Checked = check};
        item.Click += (s, e) => action();
        return item;
    }

    ContextMenuStrip CreateOptionsMenu()
    {
        // Closed runs inside ToolStrip's click processing: disposing there invalidates
        // the handle which WinForms still needs while dismissing the dropdown.
        if (optionsMenu != null)
            optionsMenu.Dispose();
        var m = optionsMenu = new ContextMenuStrip();
        m.Closed += (s, e) =>
        {
            if (!IsDisposed)
            {
                SyncContent();
                app.Changed();
            }
        };
        var colors = new ToolStripMenuItem("Цвет стикера");
        foreach (var c in new[]{Color.FromArgb(255, 231, 160), Color.FromArgb(196, 234, 209), Color.FromArgb(193, 222, 250), Color.FromArgb(247, 199, 218), Color.FromArgb(222, 207, 247), Color.White})
        {
            Color chosen = c;
            var item = new ToolStripMenuItem("● " + ColorTranslator.ToHtml(c))
            {BackColor = c};
            item.Click += (s, e) =>
            {
                Data.Color = chosen.ToArgb();
                ApplyNoteColor();
                app.Changed();
            };
            colors.DropDownItems.Add(item);
        }

        colors.DropDownItems.Add("Другой цвет…", null, (s, e) =>
        {
            using (var d = new ColorDialog{Color = BackColor, FullOpen = true})
                if (d.ShowDialog() == DialogResult.OK)
                {
                    Data.Color = d.Color.ToArgb();
                    ApplyNoteColor();
                    app.Changed();
                }
        }

        );
        m.Items.Add(colors);
        m.Items.Add("Шрифт и размер…", null, (s, e) =>
        {
            using (var d = new FontDialog{Font = editor.SelectionFont ?? editor.Font})
                if (d.ShowDialog() == DialogResult.OK)
                    editor.SelectionFont = d.Font;
        }

        );
        m.Items.Add("Жирный", null, (s, e) => ToggleTextStyle(FontStyle.Bold));
        m.Items.Add("Курсив", null, (s, e) => ToggleTextStyle(FontStyle.Italic));
        m.Items.Add("Зачёркнутый", null, (s, e) => ToggleTextStyle(FontStyle.Strikeout));
        m.Items.Add("Добавить пункт списка", null, (s, e) =>
        {
            int start = editor.GetFirstCharIndexOfCurrentLine();
            editor.Select(start, 0);
            editor.SelectedText = "☐ ";
            editor.Focus();
        }

        );
        m.Items.Add("Вставить картинку…", null, (s, e) =>
        {
            using (var d = new OpenFileDialog{Filter = Language.T("Изображения") + "|*.png;*.jpg;*.jpeg;*.bmp;*.gif"})
                if (d.ShowDialog() == DialogResult.OK)
                    InsertImage(d.FileName);
        }

        );
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(CreateToggleItem("Поверх всех окон", () =>
        {
            Data.Top = !Data.Top;
            if (Data.Top)
                Data.Desktop = false;
            TopMost = Data.Top;
            app.Changed();
        }

        , Data.Top));
        m.Items.Add(CreateToggleItem("Только рабочий стол", () =>
        {
            Data.Desktop = !Data.Desktop;
            if (Data.Desktop)
            {
                Data.Top = false;
                TopMost = false;
            }

            app.Changed();
        }

        , Data.Desktop));
        m.Items.Add(CreateToggleItem("Закрепить позицию", () =>
        {
            Data.Locked = !Data.Locked;
            app.Changed();
        }

        , Data.Locked));
        m.Items.Add(CreateToggleItem("Свернуть стикер", () => ToggleCollapsed(), Data.Folded));
        var opacity = new ToolStripMenuItem("Непрозрачность");
        foreach (int value in new[]{100, 90, 75, 50, 30})
        {
            int v = value;
            opacity.DropDownItems.Add(CreateToggleItem(v + "%", () =>
            {
                Opacity = Data.Opacity = v / 100.0;
                app.Changed();
            }

            , Math.Abs(Data.Opacity - v / 100.0) < .01));
        }

        m.Items.Add(opacity);
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add("Удалить навсегда", null, (s, e) => app.Delete(this));
        Language.Translate(m.Items);
        return m;
    }
}
