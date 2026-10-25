using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32;

[DataContract] public class NoteData {
 [DataMember] public string Id = Guid.NewGuid().ToString("N");
 [DataMember] public string Rtf = "";
 [DataMember] public int X = 100, Y = 100, W = 320, H = 340;
 [DataMember] public int Color = unchecked((int)0xFFFFE7A0);
 [DataMember] public double Opacity = 1;
 [DataMember] public bool Top, Locked, Folded;
 [DataMember] public bool Desktop = true;
}
static class Program {
 [STAThread] static void Main(string[] args) {
  Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
  Language.Load();
  bool first; using(var mutex = new System.Threading.Mutex(true,"Local\\ListikDesktopNotes",out first)) {
   if(!first) return;
   Application.Run(new NotesApp());
  }
 }
}
class NotesApp : ApplicationContext {
 public List<NoteWindow> Windows = new List<NoteWindow>();
 NotifyIcon tray; Timer timer; bool hidden, exiting, dirty; string dir; bool showWindows;
 public NotesApp(string dataDirectory=null,bool displayWindows=true) {
  showWindows=displayWindows;
  dir=dataDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Listik");
  Directory.CreateDirectory(dir);
  var menu=new ContextMenuStrip();
  menu.Items.Add("＋ Новый стикер",null,(s,e)=>Create());
  menu.Items.Add("Показать / скрыть все",null,(s,e)=>{hidden=!hidden; RefreshVisibility();});
  var startup=new ToolStripMenuItem("Запускать с Windows");
  using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) startup.Checked=key!=null && key.GetValue("Listik")!=null;
  startup.Click+=(s,e)=> { try { using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) { if(startup.Checked) key.DeleteValue("Listik",false); else key.SetValue("Listik","\""+Application.ExecutablePath+"\""); } startup.Checked=!startup.Checked; } catch(Exception ex){MessageBox.Show(ex.Message,"Автозапуск");} };
  menu.Items.Add(startup);
  var languages=new ToolStripMenuItem("Язык");
  string[] codes={"en","fr","ru","uk"}, names={"English","Français","Русский","Українська"};
  for(int i=0;i<codes.Length;i++){string code=codes[i];var option=new ToolStripMenuItem(names[i]){Checked=Language.Current==code};option.Click+=(s,e)=>{Language.Current=code;Language.Save();Language.Translate(menu.Items);foreach(ToolStripMenuItem entry in languages.DropDownItems)entry.Checked=entry==option;};languages.DropDownItems.Add(option);}
  menu.Items.Add(languages); menu.Items.Add(new ToolStripSeparator());
  menu.Items.Add("Выход",null,(s,e)=>ExitThread());
  Language.Translate(menu.Items);
  tray=new NotifyIcon {Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath), Text="Listik",ContextMenuStrip=menu,Visible=showWindows};
  tray.DoubleClick+=(s,e)=>{hidden=false;RefreshVisibility();};
  string file=Path.Combine(dir,"notes.json");
  if(File.Exists(file)) {
   try { using(var stream=File.OpenRead(file)) foreach(var data in (List<NoteData>)Serializer().ReadObject(stream)) Add(data); }
   catch(Exception ex) {MessageBox.Show(Language.T("Не удалось прочитать заметки. Исходные данные сохранены.")+"\n"+ex.Message,"Listik"); tray.Dispose(); Environment.Exit(1);}
  } else Create();
  timer=new Timer {Interval=600}; timer.Tick+=(s,e)=>{if(dirty) Save();RefreshVisibility();};timer.Start();
  if(showWindows)SystemEvents.DisplaySettingsChanged+=DisplayChanged;
 }
 DataContractJsonSerializer Serializer(){return new DataContractJsonSerializer(typeof(List<NoteData>));}
 void Add(NoteData data){var window=new NoteWindow(this,data);Windows.Add(window);if(showWindows)window.Show();}
 public void Create(){hidden=false;Add(new NoteData {X=Screen.PrimaryScreen.WorkingArea.X+80+Windows.Count%8*26,Y=Screen.PrimaryScreen.WorkingArea.Y+80+Windows.Count%8*26});Changed();}
 public void Changed(){dirty=true;}
 public void Delete(NoteWindow window){Windows.Remove(window);window.Hide();Changed();Save();if(window.IsHandleCreated)window.BeginInvoke((Action)(()=>window.Dispose()));else window.Dispose();}
 public void Save(){try { string file=Path.Combine(dir,"notes.json"),temp=file+".tmp";foreach(var w in Windows)w.SyncContent();using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){Serializer().WriteObject(stream,Windows.Select(w=>w.Data).ToList());stream.Flush(true);}Native.CommitFile(temp,file);dirty=false;}catch(Exception ex){if(!showWindows)throw;if(timer!=null)timer.Stop();MessageBox.Show(Language.T("Не удалось сохранить заметки:")+"\n"+ex.Message,"Listik");if(timer!=null)timer.Start();}}
 void DisplayChanged(object s,EventArgs e){foreach(var w in Windows.ToArray()) if(!w.IsDisposed)w.BeginInvoke((Action)(()=>w.EnsureVisible()));}
 void RefreshVisibility(){if(!showWindows)return;IntPtr fg=Native.GetForegroundWindow();bool desktop=Native.IsDesktop(fg);foreach(var w in Windows)w.ApplyVisibility(!hidden,desktop);}
 protected override void ExitThreadCore(){if(exiting)return;exiting=true;Save();timer.Stop();SystemEvents.DisplaySettingsChanged-=DisplayChanged;foreach(var w in Windows.ToArray())w.Dispose();tray.Dispose();base.ExitThreadCore();}
}
static class Native {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] public static extern bool MoveFileEx(string source,string destination,uint flags);
 public static void CommitFile(string source,string destination){
  // File scanners can briefly hold the destination without delete sharing.
  for(int attempt=0;;attempt++){
   if(MoveFileEx(source,destination,9))return;
   int error=Marshal.GetLastWin32Error();
   if(attempt>=5||(error!=5&&error!=32&&error!=33))throw new System.ComponentModel.Win32Exception(error);
   System.Threading.Thread.Sleep(50*(attempt+1));
  }
 }
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd,System.Text.StringBuilder text,int count);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd,int command);
 [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
 public static bool IsDesktop(IntPtr hwnd){var name=new System.Text.StringBuilder(256);GetClassName(hwnd,name,256);return name.ToString()=="Progman"||name.ToString()=="WorkerW";}
}
class NoteWindow : Form {
 protected override void WndProc(ref Message m){
  const int WM_NCHITTEST=0x84;
  if(m.Msg==WM_NCHITTEST){
   long packed=m.LParam.ToInt64();
   Point p=PointToClient(new Point(unchecked((short)(packed&0xffff)),unchecked((short)((packed>>16)&0xffff))));
   int edge=Padding.Left;
   bool left=p.X<edge,right=p.X>=ClientSize.Width-edge,top=p.Y<edge,bottom=p.Y>=ClientSize.Height-edge;
   int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;
   if(hit!=0){m.Result=new IntPtr(hit);return;}
  }
  base.WndProc(ref m);
 }
 public NoteData Data; NotesApp app; RichTextBox editor; Panel header; Label title; bool loading=true; Point drag; bool dragging;
 ContextMenuStrip optionsMenu;
 protected override void Dispose(bool disposing){if(disposing&&optionsMenu!=null){optionsMenu.Dispose();optionsMenu=null;}base.Dispose(disposing);}
 public NoteWindow(NotesApp owner,NoteData data){
  app=owner;Data=data;Text="Listik";FormBorderStyle=FormBorderStyle.None;Padding=new Padding(5);ControlBox=false;ShowInTaskbar=false;MinimumSize=new Size(220,80);StartPosition=FormStartPosition.Manual;
  Bounds=new Rectangle(data.X,data.Y,Math.Max(220,data.W),Math.Max(120,data.H));TopMost=data.Top;Opacity=Math.Max(.3,Math.Min(1,data.Opacity));
  header=new Panel {Dock=DockStyle.Top,Height=26};
  title=new Label {Text="",Dock=DockStyle.Fill};
  var add=Button("+",()=>app.Create());var more=Button("···",()=>Options().Show(header,new Point(0,34)));var delete=Button("×",()=>app.Delete(this));
  header.Controls.Add(title);header.Controls.Add(more);header.Controls.Add(add);header.Controls.Add(delete);
  editor=new RichTextBox {Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,Font=new Font("Segoe UI",12),DetectUrls=true,AcceptsTab=true,HideSelection=false,ScrollBars=RichTextBoxScrollBars.Vertical,AllowDrop=true};
  Controls.Add(editor);Controls.Add(header);
  if(!string.IsNullOrEmpty(data.Rtf))editor.Rtf=data.Rtf;
  editor.TextChanged+=(s,e)=>{Data.Rtf=editor.Rtf;if(!loading)app.Changed();};
  editor.LinkClicked+=(s,e)=>{Uri uri;if(Uri.TryCreate(e.LinkText,UriKind.Absolute,out uri)&&(uri.Scheme=="http"||uri.Scheme=="https"||uri.Scheme=="mailto"))try{System.Diagnostics.Process.Start(uri.AbsoluteUri);}catch(Exception ex){MessageBox.Show(ex.Message);}};
  editor.DragEnter+=(s,e)=>{if(e.Data.GetDataPresent(DataFormats.FileDrop))e.Effect=DragDropEffects.Copy;};
  editor.DragDrop+=(s,e)=>{foreach(string path in (string[])e.Data.GetData(DataFormats.FileDrop))InsertImage(path);};
  editor.MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;int i=editor.GetCharIndexFromPosition(e.Location);if(i<editor.TextLength&&(editor.Text[i]=='☐'||editor.Text[i]=='☑')){editor.Select(i,1);editor.SelectedText=editor.Text[i]=='☐'?"☑":"☐";}};
  title.MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left&&!Data.Locked){drag=e.Location;dragging=true;title.Capture=true;}};
  title.MouseMove+=(s,e)=>{if(dragging)Location=new Point(Left+e.X-drag.X,Top+e.Y-drag.Y);};title.MouseUp+=(s,e)=>{dragging=false;title.Capture=false;};title.DoubleClick+=(s,e)=>Fold();
  LocationChanged+=(s,e)=>CaptureBounds();ResizeEnd+=(s,e)=>CaptureBounds();
  FormClosing+=(s,e)=>{e.Cancel=true;Hide();};
  PaintColor();EnsureVisible();if(data.Folded){Height=80;editor.Visible=false;}loading=false;
 }
 public void SyncContent(){Data.Rtf=editor.Rtf;}
 Button Button(string text,Action action){var b=new Button {Text=text,Dock=DockStyle.Right,Width=32,FlatStyle=FlatStyle.Flat,TabStop=false};b.FlatAppearance.BorderSize=0;b.Click+=(s,e)=>action();return b;}
 void CaptureBounds(){if(loading||WindowState!=FormWindowState.Normal||Native.IsIconic(Handle))return;Data.X=Left;Data.Y=Top;Data.W=Width;if(!Data.Folded)Data.H=Height;app.Changed();}
 public void EnsureVisible(){var area=Screen.FromRectangle(Bounds).WorkingArea;Width=Math.Min(Width,area.Width);Height=Math.Min(Height,area.Height);Location=new Point(Math.Max(area.Left,Math.Min(Left,area.Right-Width)),Math.Max(area.Top,Math.Min(Top,area.Bottom-Height)));CaptureBounds();}
 public void ShowInactive(){Native.ShowWindow(Handle,4);}
 public void ApplyVisibility(bool show,bool desktop){
  if(!show){if(Visible)Hide();return;}
  // Show Desktop can minimize a window without changing its Visible property.
  // Restore without activation. Shell flyouts do not change note visibility.
  if(!Visible||Native.IsIconic(Handle))ShowInactive();
  bool onTop=Data.Top;
  if(TopMost!=onTop)TopMost=onTop;
  // HWND_TOP raises above Explorer after Show Desktop without making notes
  // topmost: Start, tray flyouts and application windows can cover them.
  if(desktop)Native.SetWindowPos(Handle,Data.Top?new IntPtr(-1):IntPtr.Zero,0,0,0,0,0x0013);
 }
 void PaintColor(){var c=Color.FromArgb(Data.Color);BackColor=header.BackColor=editor.BackColor=c;ForeColor=header.ForeColor=editor.ForeColor=c.GetBrightness()<.45?Color.White:Color.FromArgb(40,40,40);}
 void Fold(){Data.Folded=!Data.Folded;editor.Visible=!Data.Folded;Height=Data.Folded?80:Data.H;app.Changed();}
 void ToggleStyle(FontStyle style){using(var font=new Font(editor.SelectionFont??editor.Font,(editor.SelectionFont??editor.Font).Style^style))editor.SelectionFont=font;}
 void InsertImage(string path){try{using(var image=Image.FromFile(path)){Clipboard.SetImage(image);editor.Paste();}}catch(Exception ex){MessageBox.Show(Language.T("Не удалось вставить картинку:")+"\n"+ex.Message,"Listik");}}
 ToolStripMenuItem Item(string name,Action action,bool check){var item=new ToolStripMenuItem(name){Checked=check};item.Click+=(s,e)=>action();return item;}
 ContextMenuStrip Options(){
  // Closed runs inside ToolStrip's click processing: disposing there invalidates
  // the handle which WinForms still needs while dismissing the dropdown.
  if(optionsMenu!=null)optionsMenu.Dispose();
  var m=optionsMenu=new ContextMenuStrip();m.Closed+=(s,e)=>{if(!IsDisposed){SyncContent();app.Changed();}};
  var colors=new ToolStripMenuItem("Цвет стикера");foreach(var c in new[]{Color.FromArgb(255,231,160),Color.FromArgb(196,234,209),Color.FromArgb(193,222,250),Color.FromArgb(247,199,218),Color.FromArgb(222,207,247),Color.White}){Color chosen=c;var item=new ToolStripMenuItem("● "+ColorTranslator.ToHtml(c)){BackColor=c};item.Click+=(s,e)=>{Data.Color=chosen.ToArgb();PaintColor();app.Changed();};colors.DropDownItems.Add(item);}
  colors.DropDownItems.Add("Другой цвет…",null,(s,e)=>{using(var d=new ColorDialog {Color=BackColor,FullOpen=true})if(d.ShowDialog()==DialogResult.OK){Data.Color=d.Color.ToArgb();PaintColor();app.Changed();}});m.Items.Add(colors);
  m.Items.Add("Шрифт и размер…",null,(s,e)=>{using(var d=new FontDialog {Font=editor.SelectionFont??editor.Font})if(d.ShowDialog()==DialogResult.OK)editor.SelectionFont=d.Font;});
  m.Items.Add("Жирный",null,(s,e)=>ToggleStyle(FontStyle.Bold));m.Items.Add("Курсив",null,(s,e)=>ToggleStyle(FontStyle.Italic));m.Items.Add("Зачёркнутый",null,(s,e)=>ToggleStyle(FontStyle.Strikeout));
  m.Items.Add("Добавить пункт списка",null,(s,e)=>{int start=editor.GetFirstCharIndexOfCurrentLine();editor.Select(start,0);editor.SelectedText="☐ ";editor.Focus();});
  m.Items.Add("Вставить картинку…",null,(s,e)=>{using(var d=new OpenFileDialog {Filter=Language.T("Изображения")+"|*.png;*.jpg;*.jpeg;*.bmp;*.gif"})if(d.ShowDialog()==DialogResult.OK)InsertImage(d.FileName);});
  m.Items.Add(new ToolStripSeparator());
  m.Items.Add(Item("Поверх всех окон",()=>{Data.Top=!Data.Top;if(Data.Top)Data.Desktop=false;TopMost=Data.Top;app.Changed();},Data.Top));
  m.Items.Add(Item("Только рабочий стол",()=>{Data.Desktop=!Data.Desktop;if(Data.Desktop){Data.Top=false;TopMost=false;}app.Changed();},Data.Desktop));
  m.Items.Add(Item("Закрепить позицию",()=>{Data.Locked=!Data.Locked;app.Changed();},Data.Locked));
  m.Items.Add(Item("Свернуть стикер",()=>Fold(),Data.Folded));
  var opacity=new ToolStripMenuItem("Непрозрачность");foreach(int value in new[]{100,90,75,50,30}){int v=value;opacity.DropDownItems.Add(Item(v+"%",()=>{Opacity=Data.Opacity=v/100.0;app.Changed();},Math.Abs(Data.Opacity-v/100.0)<.01));}m.Items.Add(opacity);
  m.Items.Add(new ToolStripSeparator());m.Items.Add("Удалить навсегда",null,(s,e)=>app.Delete(this));Language.Translate(m.Items);return m;
 }
}
static class Language {
 public static string Current="en";
 static string PathName {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Listik","language.txt");}}
 public static void Load(){string code=System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;try{if(File.Exists(PathName))code=File.ReadAllText(PathName).Trim();}catch(IOException){}Current=new[]{"en","fr","ru","uk"}.Contains(code)?code:"en";}
 public static void Save(){try{Directory.CreateDirectory(Path.GetDirectoryName(PathName));File.WriteAllText(PathName,Current);}catch(Exception e){MessageBox.Show(e.Message,"Listik");}}
 static readonly Dictionary<string,string[]> Words=new Dictionary<string,string[]> {
  {"＋ Новый стикер",new[]{"＋ New note","＋ Nouvelle note","＋ Новий стікер"}},
  {"Показать / скрыть все",new[]{"Show / hide all","Afficher / masquer tout","Показати / приховати всі"}},
  {"Запускать с Windows",new[]{"Start with Windows","Lancer avec Windows","Запускати з Windows"}},
  {"Выход",new[]{"Quit","Quitter","Вийти"}},
  {"Язык",new[]{"Language","Langue","Мова"}},
  {"Цвет стикера",new[]{"Note colour","Couleur de la note","Колір стікера"}},
  {"Другой цвет…",new[]{"Custom colour…","Couleur personnalisée…","Інший колір…"}},
  {"Шрифт и размер…",new[]{"Font and size…","Police et taille…","Шрифт і розмір…"}},
  {"Жирный",new[]{"Bold","Gras","Жирний"}},
  {"Курсив",new[]{"Italic","Italique","Курсив"}},
  {"Зачёркнутый",new[]{"Strikethrough","Barré","Закреслений"}},
  {"Добавить пункт списка",new[]{"Add checklist item","Ajouter une case à cocher","Додати пункт списку"}},
  {"Вставить картинку…",new[]{"Insert image…","Insérer une image…","Вставити зображення…"}},
  {"Поверх всех окон",new[]{"Always on top","Toujours au premier plan","Поверх усіх вікон"}},
  {"Только рабочий стол",new[]{"Desktop only","Bureau uniquement","Лише робочий стіл"}},
  {"Закрепить позицию",new[]{"Lock position","Verrouiller la position","Закріпити позицію"}},
  {"Свернуть стикер",new[]{"Collapse note","Réduire la note","Згорнути стікер"}},
  {"Непрозрачность",new[]{"Opacity","Opacité","Непрозорість"}},
  {"Удалить навсегда",new[]{"Delete permanently","Supprimer définitivement","Видалити назавжди"}},
  {"Изображения",new[]{"Images","Images","Зображення"}},
  {"Не удалось вставить картинку:",new[]{"Could not insert image:","Impossible d’insérer l’image :","Не вдалося вставити зображення:"}},
  {"Не удалось сохранить заметки:",new[]{"Could not save notes:","Impossible d’enregistrer les notes :","Не вдалося зберегти нотатки:"}},
  {"Не удалось прочитать заметки. Исходные данные сохранены.",new[]{"Could not read notes. Original data has been preserved.","Impossible de lire les notes. Les données originales sont préservées.","Не вдалося прочитати нотатки. Початкові дані збережено."}}
 };
 public static string T(string key){string[] values;if(Current=="ru"||!Words.TryGetValue(key,out values))return key;return values[Current=="fr"?1:Current=="uk"?2:0];}
 public static void Translate(ToolStripItemCollection items){foreach(ToolStripItem item in items){if(item.Tag==null)item.Tag=item.Text;item.Text=T((string)item.Tag);var menu=item as ToolStripMenuItem;if(menu!=null)Translate(menu.DropDownItems);}}
}
