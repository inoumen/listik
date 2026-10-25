using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

partial class NotesApp
{
    void InitializeTray()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("＋ Новый стикер", null, (s, e) => Create());
        menu.Items.Add("Показать / скрыть все", null, (s, e) =>
        {
            hidden = !hidden;
            RefreshVisibility();
        }

        );
        var startup = new ToolStripMenuItem("Запускать с Windows");
        using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            startup.Checked = key != null && key.GetValue("Listik") != null;
        startup.Click += (s, e) =>
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    if (startup.Checked)
                        key.DeleteValue("Listik", false);
                    else
                        key.SetValue("Listik", "\"" + Application.ExecutablePath + "\"");
                }

                startup.Checked = !startup.Checked;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Автозапуск");
            }
        };
        menu.Items.Add(startup);
        var languages = new ToolStripMenuItem("Язык");
        string[] codes = {"en", "fr", "ru", "uk"}, names = {"English", "Français", "Русский", "Українська"};
        for (int i = 0; i < codes.Length; i++)
        {
            string code = codes[i];
            var option = new ToolStripMenuItem(names[i])
            {Checked = Language.Current == code};
            option.Click += (s, e) =>
            {
                Language.Current = code;
                Language.Save();
                Language.Translate(menu.Items);
                foreach (ToolStripMenuItem entry in languages.DropDownItems)
                    entry.Checked = entry == option;
            };
            languages.DropDownItems.Add(option);
        }

        menu.Items.Add(languages);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (s, e) => ExitThread());
        Language.Translate(menu.Items);
        tray = new NotifyIcon{Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath), Text = "Listik", ContextMenuStrip = menu, Visible = showWindows};
        tray.DoubleClick += (s, e) =>
        {
            hidden = false;
            RefreshVisibility();
        };
    }
}
