using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

static class Language
{
    public static string Current = "en";
    static string PathName
    {
        get
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Listik", "language.txt");
        }
    }

    public static void Load()
    {
        string code = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        try
        {
            if (File.Exists(PathName))
                code = File.ReadAllText(PathName).Trim();
        }
        catch (IOException)
        {
        }

        Current = new[]{"en", "fr", "ru", "uk"}.Contains(code) ? code : "en";
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathName));
            File.WriteAllText(PathName, Current);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Listik");
        }
    }

    static readonly Dictionary<string, string[]> Words = new Dictionary<string, string[]>{{"＋ Новый стикер", new[]{"＋ New note", "＋ Nouvelle note", "＋ Новий стікер"}}, {"Показать / скрыть все", new[]{"Show / hide all", "Afficher / masquer tout", "Показати / приховати всі"}}, {"Запускать с Windows", new[]{"Start with Windows", "Lancer avec Windows", "Запускати з Windows"}}, {"Выход", new[]{"Quit", "Quitter", "Вийти"}}, {"Язык", new[]{"Language", "Langue", "Мова"}}, {"Цвет стикера", new[]{"Note colour", "Couleur de la note", "Колір стікера"}}, {"Другой цвет…", new[]{"Custom colour…", "Couleur personnalisée…", "Інший колір…"}}, {"Шрифт и размер…", new[]{"Font and size…", "Police et taille…", "Шрифт і розмір…"}}, {"Жирный", new[]{"Bold", "Gras", "Жирний"}}, {"Курсив", new[]{"Italic", "Italique", "Курсив"}}, {"Зачёркнутый", new[]{"Strikethrough", "Barré", "Закреслений"}}, {"Добавить пункт списка", new[]{"Add checklist item", "Ajouter une case à cocher", "Додати пункт списку"}}, {"Вставить картинку…", new[]{"Insert image…", "Insérer une image…", "Вставити зображення…"}}, {"Поверх всех окон", new[]{"Always on top", "Toujours au premier plan", "Поверх усіх вікон"}}, {"Только рабочий стол", new[]{"Desktop only", "Bureau uniquement", "Лише робочий стіл"}}, {"Закрепить позицию", new[]{"Lock position", "Verrouiller la position", "Закріпити позицію"}}, {"Свернуть стикер", new[]{"Collapse note", "Réduire la note", "Згорнути стікер"}}, {"Непрозрачность", new[]{"Opacity", "Opacité", "Непрозорість"}}, {"Удалить навсегда", new[]{"Delete permanently", "Supprimer définitivement", "Видалити назавжди"}}, {"Изображения", new[]{"Images", "Images", "Зображення"}}, {"Не удалось вставить картинку:", new[]{"Could not insert image:", "Impossible d’insérer l’image :", "Не вдалося вставити зображення:"}}, {"Не удалось сохранить заметки:", new[]{"Could not save notes:", "Impossible d’enregistrer les notes :", "Не вдалося зберегти нотатки:"}}, {"Не удалось прочитать заметки. Исходные данные сохранены.", new[]{"Could not read notes. Original data has been preserved.", "Impossible de lire les notes. Les données originales sont préservées.", "Не вдалося прочитати нотатки. Початкові дані збережено."}}};
    public static string T(string key)
    {
        string[] values;
        if (Current == "ru" || !Words.TryGetValue(key, out values))
            return key;
        return values[Current == "fr" ? 1 : Current == "uk" ? 2 : 0];
    }

    public static void Translate(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            if (item.Tag == null)
                item.Tag = item.Text;
            item.Text = T((string)item.Tag);
            var menu = item as ToolStripMenuItem;
            if (menu != null)
                Translate(menu.DropDownItems);
        }
    }
}
