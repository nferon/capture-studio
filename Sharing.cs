using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Forms;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

public partial class Studio {
 Button shareAction;
 DataTransferManager shareManager;
 StorageFile shareFile;
 bool preparingShare;
 [ComImport,Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IDataTransferManagerInterop {
  IntPtr GetForWindow(IntPtr window,ref Guid iid);
  void ShowShareUIForWindow(IntPtr window);
 }
 void AddShareMenu(FlowLayoutPanel files){
  var popup=new ToolStripDropDown(){AutoClose=true,Padding=Padding.Empty,Margin=Padding.Empty};
  shareAction=ModernButton(files,"Partager","",delegate{},94,false);
  shareAction.Click+=delegate{if(popup.Visible){popup.Close();return;}while(popup.Items.Count>0){var item=popup.Items[0];popup.Items.RemoveAt(0);item.Dispose();}var panel=CreateSharePanel(delegate{popup.Close();});popup.Items.Add(new ToolStripControlHost(panel){AutoSize=false,Size=panel.Size,Padding=Padding.Empty,Margin=Padding.Empty});popup.Show(shareAction,new Point(shareAction.Width-popup.Width,shareAction.Height+6));};
  FormClosed+=delegate{popup.Dispose();};
  tips.SetToolTip(shareAction,"Partager l’image annotée avec une application compatible, ou la copier");
  FormClosed+=delegate{if(shareManager!=null){shareManager.DataRequested-=OnShareDataRequested;shareManager=null;}shareFile=null;};
 }
 void ChooseShareFavorites(){
  using(var dialog=new Form(){Icon=Icon,Text="Favoris de partage",ClientSize=new Size(430,245),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false,Font=Font}){
   var hint=new Label(){Text="Copier votre image puis ouvrir l’application.\nChoisissez ensuite la conversation et collez avec Ctrl+V.",Location=new Point(20,16),Size=new Size(390,55)};dialog.Controls.Add(hint);
   var discord=new CheckBox(){Text="Discord",Checked=Array.IndexOf(CaptureHost.preferences.ShareFavorites??new string[0],"Discord")>=0,Location=new Point(20,82),AutoSize=true};dialog.Controls.Add(discord);
   var whatsapp=new CheckBox(){Text="WhatsApp",Checked=Array.IndexOf(CaptureHost.preferences.ShareFavorites??new string[0],"WhatsApp")>=0,Location=new Point(20,120),AutoSize=true};dialog.Controls.Add(whatsapp);
   var save=new Button(){Text="Enregistrer",Location=new Point(175,190),Size=new Size(110,32),DialogResult=DialogResult.OK};var cancel=new Button(){Text="Annuler",Location=new Point(295,190),Size=new Size(110,32),DialogResult=DialogResult.Cancel};dialog.Controls.Add(save);dialog.Controls.Add(cancel);dialog.AcceptButton=save;dialog.CancelButton=cancel;
   if(dialog.ShowDialog(this)==DialogResult.OK){var chosen=new System.Collections.Generic.List<string>();if(discord.Checked)chosen.Add("Discord");if(whatsapp.Checked)chosen.Add("WhatsApp");CaptureHost.preferences.ShareFavorites=chosen.ToArray();CaptureHost.SavePreferences();}
  }
 }
 void CopyForFavorite(string app){
  FinishInlineText(true);if(photo==null)return;
  try{using(var bitmap=Render())System.Windows.Forms.Clipboard.SetImage(bitmap);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Copie impossible");return;}
  try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(app=="Discord"?"discord:":"whatsapp:"){UseShellExecute=true});status.Text="Image copiée — choisissez une conversation dans "+app+" puis Ctrl+V.";}
  catch{status.Text="Image copiée. Ouvrez "+app+", choisissez la conversation puis Ctrl+V.";MessageBox.Show(this,"L’image est copiée. Ouvrez "+app+" manuellement, choisissez la conversation et collez avec Ctrl+V.","Ouvrir "+app);}
 }
 // Keep the exported snapshot independent of further edits while the share sheet is open.
 string CreateShareSnapshot(){
  FinishInlineText(true);
  if(photo==null)throw new InvalidOperationException("Ouvrez une image ou faites une capture avant de partager.");
  string directory=Path.Combine(Path.GetTempPath(),"CaptureStudio","Partages");Directory.CreateDirectory(directory);
  foreach(string old in Directory.GetFiles(directory,"Capture-*.png"))try{if(File.GetLastWriteTimeUtc(old)<DateTime.UtcNow.AddDays(-1))File.Delete(old);}catch{}
  string path=Path.Combine(directory,"Capture-"+Guid.NewGuid().ToString("N")+".png");
  using(var bitmap=Render())bitmap.Save(path,ImageFormat.Png);return path;
 }
  System.Threading.Tasks.Task<StorageFile> ResolveShareFile(string path){
  var completion=new System.Threading.Tasks.TaskCompletionSource<StorageFile>();
  var operation=StorageFile.GetFileFromPathAsync(path);
  operation.Completed=delegate(Windows.Foundation.IAsyncOperation<StorageFile> result,Windows.Foundation.AsyncStatus state){try{completion.SetResult(result.GetResults());}catch(Exception ex){completion.SetException(ex);}};
  return completion.Task;
 }
 async void ShareWithWindows(){
  if(preparingShare||photo==null)return;preparingShare=true;shareAction.Enabled=false;
  try{
   string path=CreateShareSnapshot();var file=await ResolveShareFile(path);
   if(IsDisposed)return;
   var interop=InitializeShareManager();
   shareFile=file;interop.ShowShareUIForWindow(Handle);status.Text="Choisissez une application dans le panneau Partager de Windows.";
  }catch(Exception ex){status.Text="Partage Windows indisponible. Vous pouvez utiliser Copier ou Exporter.";MessageBox.Show(this,"Impossible d’ouvrir le partage Windows. Utilisez Copier pour coller l’image dans Discord ou une autre application, ou Exporter pour obtenir un PNG.\n\n"+ex.Message,"Partager l’image");}
  finally{preparingShare=false;if(!IsDisposed)RefreshEditorActions();}
 }
 IDataTransferManagerInterop InitializeShareManager(){
  var interop=(IDataTransferManagerInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(DataTransferManager));
  if(shareManager==null){Guid iid=new Guid("A5CAEE9B-8708-49D1-8D36-67D25A8DA00C");IntPtr pointer=interop.GetForWindow(Handle,ref iid);try{shareManager=(DataTransferManager)Marshal.GetObjectForIUnknown(pointer);}finally{Marshal.Release(pointer);}shareManager.DataRequested+=OnShareDataRequested;}return interop;
 }
 void FillSharePackage(DataPackage data,StorageFile file){data.Properties.Title="Capture Studio";data.Properties.Description="Capture avec annotations";data.SetStorageItems(new IStorageItem[]{file});data.SetBitmap(RandomAccessStreamReference.CreateFromFile(file));}
 void OnShareDataRequested(DataTransferManager sender,DataRequestedEventArgs args){
  if(shareFile==null){args.Request.FailWithDisplayText("Préparez une image avant de partager.");return;}
  try{FillSharePackage(args.Request.Data,shareFile);}catch{args.Request.FailWithDisplayText("L’image n’est plus disponible. Relancez le partage depuis Capture Studio.");}
 }
 void ConfigureFileToolbarLayout(TableLayoutPanel root,TableLayoutPanel header,FlowLayoutPanel captureBar,FlowLayoutPanel files){
  header.RowCount=2;header.RowStyles.Add(new RowStyle(SizeType.Absolute,56));header.RowStyles.Add(new RowStyle(SizeType.Absolute,0));
  Action arrange=delegate{
   bool compact=ClientSize.Width<1200;header.SuspendLayout();
   header.ColumnStyles[1].Width=compact?0:524;
   if(compact){header.SetCellPosition(files,new TableLayoutPanelCellPosition(0,1));header.SetColumnSpan(files,2);header.SetColumnSpan(captureBar,2);}
   else{header.SetColumnSpan(captureBar,1);header.SetColumnSpan(files,1);header.SetCellPosition(files,new TableLayoutPanelCellPosition(1,0));}
   header.RowStyles[1].Height=compact?52:0;root.RowStyles[0].Height=compact?128:76;
   header.ResumeLayout(true);
  };Resize+=delegate{arrange();};arrange();
 }
}


