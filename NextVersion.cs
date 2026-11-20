using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

public partial class Studio {
 bool imageSelected,spaceHeld,explicitQuit;MouseButtons panButton;
 bool TextInputFocused(){Control control=ActiveControl;while(control is ContainerControl&&((ContainerControl)control).ActiveControl!=null)control=((ContainerControl)control).ActiveControl;return textTarget!=null||control is TextBoxBase||control is UpDownBase||inlineText.ContainsFocus;}
 bool HandleMainShortcuts(KeyEventArgs e){
  if(e.Control&&!e.Alt&&!e.Shift&&e.KeyCode==Keys.O){FinishInlineText(true);OpenProject();e.SuppressKeyPress=true;return true;}
  if(TryExportShortcut(e.KeyData)){e.SuppressKeyPress=true;return true;}
  if(TextInputFocused())return false;
  if(e.KeyCode==Keys.Space&&!e.Control&&!e.Alt){spaceHeld=true;canvas.Cursor=Cursors.Hand;e.SuppressKeyPress=true;return true;}
  if(!e.Control)return false;
  if(e.KeyCode==Keys.A){SelectImage();e.SuppressKeyPress=true;return true;}
  if(e.KeyCode==Keys.C){CopyCompleteImage();e.SuppressKeyPress=true;return true;}
  if(e.KeyCode==Keys.V){PasteImage();e.SuppressKeyPress=true;return true;}return false;
 }
 protected virtual void RunExportShortcut(){FinishInlineText(true);Export();}
 bool TryExportShortcut(Keys keys){
  bool primary=keys==(Keys.Control|Keys.Shift|Keys.S);
  bool alternate=keys==(Keys.Alt|Keys.Shift|Keys.S);
  if(!primary&&!alternate)return false;
  RunExportShortcut();return true;
 }
 protected override bool ProcessCmdKey(ref Message message,Keys keyData){if(keyData==(Keys.Control|Keys.N)){OpenNewWindow(new string[0]);return true;}if(TryExportShortcut(keyData))return true;return base.ProcessCmdKey(ref message,keyData);}
 void SelectImage(){if(photo==null)return;selected=null;selection.Clear();imageSelected=true;RefreshSelection();canvas.Invalidate();status.Text="Image entière sélectionnée • Ctrl+C pour la copier avec ses annotations";}
 DataObject BuildImageClipboard(){if(photo==null)return null;using(var bitmap=Render()){var data=new DataObject();data.SetData(DataFormats.Bitmap,true,new Bitmap(bitmap));using(var png=new MemoryStream()){bitmap.Save(png,System.Drawing.Imaging.ImageFormat.Png);data.SetData("PNG",false,new MemoryStream(png.ToArray()));}return data;}}
 void CopyCompleteImage(){if(photo==null)return;FinishInlineText(true);DataObject data=null;try{data=BuildImageClipboard();Clipboard.SetDataObject(data,true,5,80);status.Text="Image copiée avec ses annotations • Ctrl+V dans Discord ou une autre application";}catch(Exception ex){status.Text="Copie indisponible : "+ex.Message;}finally{if(data!=null){var bitmap=data.GetData(DataFormats.Bitmap) as IDisposable;if(bitmap!=null)bitmap.Dispose();var png=data.GetData("PNG") as IDisposable;if(png!=null)png.Dispose();}}}
 void DrawImageSelection(Graphics g){if(!imageSelected)return;var r=ViewRect;using(var pen=new Pen(Color.FromArgb(115,87,255),2/scale)){pen.DashStyle=System.Drawing.Drawing2D.DashStyle.Dash;g.DrawRectangle(pen,r.Left+1/scale,r.Top+1/scale,r.Width-2/scale,r.Height-2/scale);}}
 void InitializeNextVersion(){
  KeyUp+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Space){spaceHeld=false;if(panning&&panButton==MouseButtons.Left)EndPan();if(!panning)canvas.Cursor=Cursors.Default;}};
  Deactivate+=delegate{spaceHeld=false;if(panning)EndPan();};
 }
 void QuitApplication(){if(captureHost!=null){Close();return;}var windows=CaptureWindows();foreach(var window in windows){window.FinishInlineText(true);if(!window.ConfirmDiscard())return;}closingCaptureWindows=true;foreach(var window in windows)if(window!=this){window.explicitQuit=true;window.Close();}explicitQuit=true;Close();if(!IsDisposed)explicitQuit=false;closingCaptureWindows=false;}
 void HandleWindowClosing(FormClosingEventArgs e){
  SaveWindowPlacement();
  if(closingCaptureWindows)return;
  if(captureHost==null&&e.CloseReason==CloseReason.UserClosing&&!explicitQuit&&CaptureWindows().Count>1){FinishInlineText(true);SaveDraft();EnsureTray();Hide();e.Cancel=true;return;}
  if(captureHost!=null){if(e.CloseReason==CloseReason.UserClosing)e.Cancel=!ConfirmDiscard();return;}
  if(e.CloseReason==CloseReason.UserClosing&&!explicitQuit&&preferences.CloseToTray){FinishInlineText(true);SaveDraft();SavePreferences();EnsureTray();Hide();e.Cancel=true;return;}
  if(e.CloseReason==CloseReason.UserClosing)e.Cancel=!ConfirmDiscard();
 }
 void SaveWindowPlacement(){if(!preferences.RememberWindow)return;Rectangle r=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;if(r.Width<1||r.Height<1)return;preferences.WindowX=r.X;preferences.WindowY=r.Y;preferences.WindowW=r.Width;preferences.WindowH=r.Height;preferences.Maximized=WindowState==FormWindowState.Maximized;}
 public static Rectangle VisiblePlacement(Rectangle saved,Rectangle[] areas){
  if(areas==null||areas.Length==0)return saved;Rectangle area=areas.OrderByDescending(a=>{var intersection=Rectangle.Intersect(a,saved);return (long)Math.Max(0,intersection.Width)*Math.Max(0,intersection.Height);}).First();
  int width=Math.Min(area.Width,Math.Max(600,saved.Width)),height=Math.Min(area.Height,Math.Max(400,saved.Height));int x=Math.Max(area.Left,Math.Min(area.Right-width,saved.X)),y=Math.Max(area.Top,Math.Min(area.Bottom-height,saved.Y));return new Rectangle(x,y,width,height);
 }
 void RestoreWindowPlacement(){if(!preferences.RememberWindow||preferences.WindowW<=0)return;var r=VisiblePlacement(new Rectangle(preferences.WindowX,preferences.WindowY,preferences.WindowW,preferences.WindowH),Screen.AllScreens.Select(s=>s.WorkingArea).ToArray());MinimumSize=new Size(Math.Min(MinimumSize.Width,r.Width),Math.Min(MinimumSize.Height,r.Height));StartPosition=FormStartPosition.Manual;Bounds=r;if(preferences.Maximized)WindowState=FormWindowState.Maximized;}
 bool IsStartupEnabled(){using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))return key!=null&&key.GetValue("CaptureStudio")!=null;}
 void SetStartup(bool enabled){using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){if(enabled)key.SetValue("CaptureStudio",QuoteArgument(System.Reflection.Assembly.GetExecutingAssembly().Location)+" --background");else key.DeleteValue("CaptureStudio",false);}}
 void ShowSettings(){if(captureHost!=null){captureHost.ShowSettings();return;}using(var dialog=CreateSettingsDialog())dialog.ShowDialog(this);}
 Form CreateSettingsDialog(){
  var dialog=new Form(){Icon=Icon,Text="Paramètres — Capture Studio",Width=620,Height=860,MinimumSize=new Size(540,580),StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false,Font=Font,BackColor=StudioTheme.Background};{
   var panel=new FlowLayoutPanel(){Dock=DockStyle.Fill,Padding=new Padding(24),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=StudioTheme.Background};dialog.Controls.Add(panel);
   Heading(panel,"COMPORTEMENT");
   var startup=new CheckBox(){Text="Démarrer avec Windows, près de l’horloge",Checked=IsStartupEnabled(),AutoSize=true,ForeColor=StudioTheme.Text,Margin=new Padding(3,12,3,6)};panel.Controls.Add(startup);
   var trayClose=new CheckBox(){Text="La croix ferme la fenêtre mais garde l’application près de l’horloge",Checked=preferences.CloseToTray,AutoSize=true,ForeColor=StudioTheme.Text,Margin=new Padding(3,8,3,6)};panel.Controls.Add(trayClose);
   var remember=new CheckBox(){Text="Mémoriser la taille, la position et l’écran de la fenêtre",Checked=preferences.RememberWindow,AutoSize=true,ForeColor=StudioTheme.Text,Margin=new Padding(3,8,3,6)};panel.Controls.Add(remember);
   var multiple=new CheckBox(){Text="Ouvrir les nouvelles captures dans des fenêtres séparées",Checked=preferences.MultipleWindows,AutoSize=true,ForeColor=StudioTheme.Text,Margin=new Padding(3,8,3,6)};panel.Controls.Add(multiple);
   var updateCheck=new CheckBox(){Text="Vérifier les mises à jour au démarrage",Checked=preferences.CheckUpdates,AutoSize=true,ForeColor=StudioTheme.Text,Margin=new Padding(3,8,3,6)};panel.Controls.Add(updateCheck);
   var shortcut=AddShortcutSettings(panel);
   Heading(panel,"APPARENCE");var theme=new ComboBox(){DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,Width=240,BackColor=StudioTheme.Surface,ForeColor=StudioTheme.Text};theme.Items.AddRange(new object[]{"Système","Clair","Sombre"});theme.SelectedItem=preferences.Theme;if(theme.SelectedIndex<0)theme.SelectedIndex=0;panel.Controls.Add(theme);
   Heading(panel,"DOSSIER DES CAPTURES PNG");
   var captureFolder=new TextBox(){ReadOnly=true,Text=CaptureDirectoryPath(),Width=500};panel.Controls.Add(captureFolder);
   ModernButton(panel,"Choisir un dossier…","",delegate{using(var picker=new FolderBrowserDialog(){SelectedPath=captureFolder.Text,Description="Dossier par défaut des captures PNG"})if(picker.ShowDialog(dialog)==DialogResult.OK)captureFolder.Text=picker.SelectedPath;},240,false);
   ModernButton(panel,"Ouvrir le dossier des captures","",delegate{try{System.IO.Directory.CreateDirectory(captureFolder.Text);System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(captureFolder.Text){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(dialog,ex.Message,"Dossier indisponible");}},280,false);
   AddFileAssociationSettings(panel,dialog);
   var buttons=new FlowLayoutPanel(){Width=520,Height=52,Margin=new Padding(0,16,0,0)};panel.Controls.Add(buttons);
   ModernButton(buttons,"Enregistrer","",delegate{try{string shortcutError;if(!ApplyCaptureShortcut(shortcut.Enabled.Checked,shortcut.Key,shortcut.Modifiers,out shortcutError,true)){shortcut.Status.Text=shortcutError;MessageBox.Show(dialog,shortcutError,"Raccourci indisponible");return;}if(startup.Checked!=IsStartupEnabled())SetStartup(startup.Checked);System.IO.Directory.CreateDirectory(captureFolder.Text);preferences.CaptureDirectory=captureFolder.Text;preferences.CheckUpdates=updateCheck.Checked;preferences.MultipleWindows=multiple.Checked;preferences.CloseToTray=trayClose.Checked;preferences.RememberWindow=remember.Checked;preferences.Theme=theme.Text;SaveWindowPlacement();SavePreferences();ApplyWindowsTheme();dialog.DialogResult=DialogResult.OK;dialog.Close();}catch(Exception ex){MessageBox.Show(dialog,ex.Message,"Paramètres non enregistrés");}},180,true);
   ModernButton(buttons,"Annuler","",delegate{dialog.Close();},120,false);buttons.Dock=DockStyle.Bottom;buttons.Height=76;buttons.Padding=new Padding(24,10,0,0);buttons.BackColor=StudioTheme.Background;dialog.Controls.Add(buttons);var work=Screen.FromControl(this).WorkingArea;dialog.MinimumSize=new Size(Math.Min(540,work.Width-32),Math.Min(580,work.Height-40));dialog.Height=Math.Min(dialog.Height,work.Height-40);FitSettingsContent(panel,buttons);return dialog;
  }
 }
}
