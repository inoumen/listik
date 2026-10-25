using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

partial class Setup
{
    static string T(int language, string en, string fr, string ru, string uk)
    {
        return new[]{en, fr, ru, uk}[language];
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        string code = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Listik");
        using (var form = new Form{Text = "Listik Setup", ClientSize = new Size(620, 450), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, StartPosition = FormStartPosition.CenterScreen, Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(43, 64, 57), BackColor = Color.FromArgb(248, 249, 243)})
        {
            form.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            var banner = new Panel{Bounds = new Rectangle(0, 0, 620, 150), BackColor = Color.FromArgb(228, 239, 214)};
            var artwork = new PictureBox{Bounds = new Rectangle(30, 29, 88, 88), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent};
            using (var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Listik-icon.png"))
                using (var original = new Bitmap(iconStream))
                    artwork.Image = new Bitmap(original);
            var languages = new ComboBox{Bounds = new Rectangle(407, 28, 183, 30), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat};
            languages.Items.AddRange(new object[]{"English", "Français", "Русский", "Українська"});
            languages.SelectedIndex = code == "fr" ? 1 : code == "ru" ? 2 : code == "uk" ? 3 : 0;
            var title = new Label{Text = "Listik", Font = new Font("Segoe UI Semibold", 32, FontStyle.Bold), Bounds = new Rectangle(138, 38, 260, 65), BackColor = Color.Transparent};
            var description = new Label{Bounds = new Rectangle(30, 178, 560, 92)};
            var path = new TextBox{ReadOnly = true, Text = folder, Bounds = new Rectangle(30, 280, 560, 30), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, ForeColor = Color.FromArgb(75, 90, 82), Font = new Font("Segoe UI", 10)};
            var launch = new CheckBox{Bounds = new Rectangle(30, 330, 560, 29), Visible = false, Checked = true};
            var button = new Button{Bounds = new Rectangle(365, 382, 225, 44), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(45, 108, 83), ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold), Cursor = Cursors.Hand};
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(59, 128, 99);
            banner.Controls.AddRange(new Control[]{artwork, title, languages});
            bool installed = false;
            bool updating = File.Exists(Path.Combine(folder, "Listik.exe"));
            Action translate = () =>
            {
                int i = languages.SelectedIndex;
                description.Text = installed ? T(i, "Installed! Find Listik in the Start menu. Your notes are preserved.", "Installation terminée ! Listik est dans le menu Démarrer. Vos notes sont conservées.", "Готово! Listik доступен в меню «Пуск». Ваши заметки сохранены.", "Готово! Listik доступний у меню «Пуск». Ваші нотатки збережено.") : T(i, "Install for your Windows account, with a Start menu shortcut. Close Listik before updating. Installation folder:", "Installation pour votre compte Windows avec un raccourci dans Démarrer. Fermez Listik avant la mise à jour. Dossier :", "Установка для вашего пользователя Windows и ярлык в меню «Пуск». Перед обновлением закройте Listik. Папка установки:", "Встановлення для вашого користувача Windows та ярлик у меню «Пуск». Перед оновленням закрийте Listik. Папка встановлення:");
                button.Text = installed ? T(i, "Finish", "Terminer", "Готово", "Готово") : updating ? T(i, "Update", "Mettre à jour", "Обновить", "Оновити") : T(i, "Install", "Installer", "Установить", "Встановити");
                if (updating && !installed)
                    description.Text = T(i, "Listik is already installed. Close it to update the application. Notes and settings will be preserved. Installation folder:", "Listik est déjà installé. Fermez-le pour le mettre à jour. Notes et réglages conservés. Dossier :", "Listik уже установлен. Закройте его для обновления. Заметки и настройки сохранятся. Папка установки:", "Listik уже встановлено. Закрийте його для оновлення. Нотатки та налаштування збережуться. Папка встановлення:");
                launch.Text = T(i, "Launch Listik", "Lancer Listik", "Запустить Listik", "Запустити Listik");
            };
            languages.SelectedIndexChanged += (s, e) => translate();
            button.Click += (s, e) =>
            {
                try
                {
                    if (installed)
                    {
                        if (launch.Checked)
                            Process.Start(Path.Combine(folder, "Listik.exe"));
                        form.Close();
                        return;
                    }

                    button.Enabled = false;
                    Install();
                    installed = true;
                    launch.Visible = true;
                    translate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(form, T(languages.SelectedIndex, "Installation failed. Close Listik and try again.", "Installation impossible. Fermez Listik et réessayez.", "Не удалось установить. Закройте Listik и попробуйте снова.", "Не вдалося встановити. Закрийте Listik і спробуйте ще раз.") + "\n" + ex.Message, "Listik", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    button.Enabled = true;
                }
            };
            form.Controls.AddRange(new Control[]{banner, description, path, launch, button});
            form.AcceptButton = button;
            translate();
            Application.Run(form);
        }
    }

}
