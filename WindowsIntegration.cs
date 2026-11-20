using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

public partial class Studio {
 static string InstanceName {get{return "CaptureStudio."+WindowsIdentity.GetCurrent().User.Value+"."+System.Diagnostics.Process.GetCurrentProcess().SessionId;}}
 NamedPipeServerStream instanceServer;bool instanceClosing,startInBackground;string[] startupArguments=new string[0];bool suppressRecovery;
 public static int RunWindowsApp(string[] args){
  bool first;using(var mutex=new Mutex(true,"Local\\"+InstanceName,out first)){
   if(!first){try{using(var client=new NamedPipeClientStream(".",InstanceName,PipeDirection.Out)){client.Connect(5000);using(var writer=new StreamWriter(client)){writer.WriteLine(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(args));writer.Flush();}}return 0;}catch(Exception ex){MessageBox.Show("Capture Studio est déjà lancé, mais ne répond pas encore. Réessayez dans quelques secondes.\n"+ex.Message);return 1;}}
   try{SetDesktopIdentity();WindowCapture.EnableDpi();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);var form=new Studio();form.startupArguments=args;form.startInBackground=args.Contains("--background");form.suppressRecovery=args.Length>0;form.InitializePersistence(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CaptureStudio"));form.InitializeWindowsSession();var handle=form.Handle;form.StartInstanceServer();Application.Run(form);return 0;}finally{mutex.ReleaseMutex();}
  }
 }
 void StartInstanceServer(){
  var security=new PipeSecurity();security.SetAccessRuleProtection(true,false);security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,PipeAccessRights.FullControl,AccessControlType.Allow));
  instanceServer=new NamedPipeServerStream(InstanceName,PipeDirection.In,1,PipeTransmissionMode.Byte,PipeOptions.None,4096,4096,security);
  var thread=new Thread(delegate(){while(!instanceClosing){try{instanceServer.WaitForConnection();string message;using(var reader=new StreamReader(instanceServer,System.Text.Encoding.UTF8,false,4096,true)){char[] buffer=new char[32769];int count=0,c;while(count<buffer.Length&&(c=reader.Read())>=0&&c!='\n')buffer[count++]=(char)c;if(count>32768)throw new InvalidDataException("Requête trop longue.");message=new string(buffer,0,count);}instanceServer.Disconnect();var forwarded=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<string[]>(message);if(!instanceClosing&&IsHandleCreated)BeginInvoke(new Action(delegate{if(!Enabled||capturing){Show();Activate();return;}HandleLaunch(forwarded??new string[0],true);}));}catch{if(instanceClosing)return;try{if(instanceServer.IsConnected)instanceServer.Disconnect();}catch{}}}}){IsBackground=true,Name="CaptureStudio instance"};thread.Start();
  FormClosed+=delegate{instanceClosing=true;instanceServer.Dispose();};
 }
 void InitializeWindowsSession(){
  RestoreWindowPlacement();ApplyWindowsTheme();UpdateJumpList();try{EnsureCaptureDirectory();}catch(Exception ex){status.Text="Dossier des captures indisponible : "+ex.Message;}
  Microsoft.Win32.UserPreferenceChangedEventHandler changed=delegate{if(!IsDisposed&&IsHandleCreated)BeginInvoke(new Action(ApplyWindowsTheme));};SystemEvents.UserPreferenceChanged+=changed;FormClosed+=delegate{SystemEvents.UserPreferenceChanged-=changed;};
  Shown+=delegate{if(preferences.Hotkey){EnsureTray();TryRestoreHotkey();}if(startInBackground){EnsureTray();Hide();}else HandleLaunch(startupArguments);CheckStartupUpdate();};
 }
 void HandleLaunch(string[] args,bool forwarded=false){
  if(forwarded&&(args.Length==0||args.Any(a=>!a.StartsWith("--",StringComparison.Ordinal)))){OpenNewWindow(args);return;}
  if(args.Contains("--background"))return;Show();if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;WindowCapture.SetForegroundWindow(Handle);Activate();
  if(args.Contains("--resume-update")){RestoreUpdateSession();return;}
  string file=args.FirstOrDefault(a=>!a.StartsWith("--",StringComparison.Ordinal));if(file!=null){try{OpenExternalFile(file);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Ouverture impossible");}return;}
  if(args.Contains("--capture-window")){captureMode.SelectedIndex=1;TakeCapture();}else if(args.Contains("--capture"))TakeCapture();
 }
 public static bool SupportedFile(string path){return new string[]{".capture",".png",".jpg",".jpeg",".bmp"}.Contains(Path.GetExtension(path).ToLowerInvariant());}
 bool OpenExternalFile(string path){
  path=Path.GetFullPath(path);if(!SupportedFile(path))throw new InvalidDataException("Formats pris en charge : .capture, PNG, JPEG et BMP.");if(!File.Exists(path))throw new FileNotFoundException("Ce fichier n’existe plus.",path);
  if(Path.GetExtension(path).Equals(".capture",StringComparison.OrdinalIgnoreCase)){var project=json.Deserialize<Project>(File.ReadAllText(path));if(project==null||project.Marks==null)throw new InvalidDataException("Projet non valide.");using(var stream=new MemoryStream(Convert.FromBase64String(project.Image)))using(var decoded=new Bitmap(stream)){var bitmap=new Bitmap(decoded);if(!ConfirmDiscard()){bitmap.Dispose();return false;}SetPhoto(bitmap);}marks=project.Marks;crop=Rectangle.Intersect(new Rectangle(project.CropX,project.CropY,Math.Max(0,project.CropWidth),Math.Max(0,project.CropHeight)),new Rectangle(Point.Empty,photo.Size));blurAmount=Math.Max(0,Math.Min(24,project.Blur));projectPath=path;RefreshSelection();ResetViewport();canvas.Invalidate();RememberProject(path);}
  else using(var image=Image.FromFile(path)){var bitmap=new Bitmap(image);if(!ConfirmDiscard()){bitmap.Dispose();return false;}SetPhoto(bitmap);}return true;
 }
 bool ReplaceWithImage(Image image){if(image==null)return false;var bitmap=new Bitmap(image);if(!ConfirmDiscard()){bitmap.Dispose();return false;}SetPhoto(bitmap);return true;}
 void PasteImage(){if(inlineText.ContainsFocus)return;try{if(!Clipboard.ContainsImage()){status.Text="Le presse-papiers ne contient pas d’image.";return;}using(var image=Clipboard.GetImage())if(ReplaceWithImage(image))status.Text="Image collée depuis le presse-papiers.";}catch(Exception ex){status.Text="Collage indisponible : "+ex.Message;}}
 void EnableDrop(Control control){control.AllowDrop=true;control.DragEnter+=delegate(object sender,DragEventArgs e){var files=e.Data.GetData(DataFormats.FileDrop) as string[];e.Effect=files!=null&&files.Length==1&&SupportedFile(files[0])?DragDropEffects.Copy:DragDropEffects.None;};control.DragDrop+=delegate(object sender,DragEventArgs e){var files=e.Data.GetData(DataFormats.FileDrop) as string[];if(files==null||files.Length!=1)return;try{OpenExternalFile(files[0]);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Ouverture impossible");}};}
 void InitializeWindowsMenu(ContextMenuStrip menu){EnableDrop(this);EnableDrop(canvas);menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Copier l’image (Ctrl+C)",null,delegate{CopyCompleteImage();});menu.Items.Add("Coller une image (Ctrl+V)",null,delegate{PasteImage();});menu.Items.Add("Rechercher les mises à jour…",null,async delegate{await CheckUpdates(true);});menu.Items.Add("Paramètres…",null,delegate{ShowSettings();});}
 static string QuoteArgument(string value){if(value.IndexOf('"')>=0)throw new ArgumentException("Nom de fichier non valide.");return "\""+value+"\"";}
 [System.Runtime.InteropServices.DllImport("shell32.dll")]static extern void SHChangeNotify(uint id,uint flags,IntPtr first,IntPtr second);
 void RegisterWindowsFiles(){RegisterWindowsFilesUnder(Registry.CurrentUser);SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);}
 void RegisterWindowsFilesUnder(RegistryKey registryRoot){
  string exe=System.Reflection.Assembly.GetExecutingAssembly().Location,command=QuoteArgument(exe)+" \"%1\"";
  string root=@"Software\Classes\";
  using(var key=registryRoot.CreateSubKey(root+"CaptureStudio.Project")){key.SetValue("","Projet Capture Studio");key.SetValue("AppUserModelID",ApplicationId);}using(var key=registryRoot.CreateSubKey(root+@"CaptureStudio.Project\DefaultIcon"))key.SetValue("",QuoteArgument(exe)+",0");using(var key=registryRoot.CreateSubKey(root+@"CaptureStudio.Project\shell\open\command"))key.SetValue("",command);
  using(var key=registryRoot.CreateSubKey(root+".capture"))if(key.GetValue("")==null)key.SetValue("","CaptureStudio.Project");
  using(var key=registryRoot.CreateSubKey(root+@"Applications\CaptureStudio.exe"))key.SetValue("FriendlyAppName","Capture Studio");using(var key=registryRoot.CreateSubKey(root+@"Applications\CaptureStudio.exe\shell\open\command"))key.SetValue("",command);
  foreach(string extension in new string[]{".capture",".png",".jpg",".jpeg",".bmp"}){using(var key=registryRoot.CreateSubKey(root+extension+@"\OpenWithProgids"))key.SetValue("CaptureStudio.Project",new byte[0],RegistryValueKind.None);using(var key=registryRoot.CreateSubKey(root+@"Applications\CaptureStudio.exe\SupportedTypes"))key.SetValue(extension,"");using(var key=registryRoot.CreateSubKey(@"Software\CaptureStudio\Capabilities\FileAssociations"))key.SetValue(extension,"CaptureStudio.Project");}
  using(var key=registryRoot.CreateSubKey(@"Software\CaptureStudio\Capabilities")){key.SetValue("ApplicationName","Capture Studio");key.SetValue("ApplicationDescription","Capture et annotation d’images et de projets");key.SetValue("ApplicationIcon",QuoteArgument(exe)+",0");}using(var key=registryRoot.CreateSubKey(@"Software\RegisteredApplications"))key.SetValue("CaptureStudio",@"Software\CaptureStudio\Capabilities");
 }
 void RememberProject(string path){if(preferences.Recent==null)preferences.Recent=new string[0];preferences.Recent=new string[]{path}.Concat(preferences.Recent.Where(p=>!String.Equals(p,path,StringComparison.OrdinalIgnoreCase))).Take(6).ToArray();SavePreferences();if(persistenceEnabled)UpdateJumpList();}
 void UpdateJumpList(){try{var list=new System.Windows.Shell.JumpList();string exe=System.Reflection.Assembly.GetExecutingAssembly().Location;list.JumpItems.Add(new System.Windows.Shell.JumpTask(){Title="Nouvelle capture",ApplicationPath=exe,Arguments="--capture",IconResourcePath=exe,IconResourceIndex=0});list.JumpItems.Add(new System.Windows.Shell.JumpTask(){Title="Capturer une fenêtre",ApplicationPath=exe,Arguments="--capture-window",IconResourcePath=exe,IconResourceIndex=0});if(preferences.Recent!=null)foreach(string path in preferences.Recent.Where(File.Exists).Take(6))list.JumpItems.Add(new System.Windows.Shell.JumpTask(){Title=Path.GetFileNameWithoutExtension(path),Description=path,ApplicationPath=exe,Arguments=QuoteArgument(path),CustomCategory="Projets récents",IconResourcePath=exe,IconResourceIndex=0});list.Apply();}catch(Exception ex){status.Text="Actions de barre des tâches indisponibles : "+ex.Message;}}
 void TryRestoreHotkey(){string error;if(!ApplyCaptureShortcut(true,preferences.HotkeyKey,preferences.HotkeyModifiers,out error,false)){status.Text=error;if(tray!=null)tray.ShowBalloonTip(5000,"Raccourci de capture indisponible",error,ToolTipIcon.Info);}}
}
