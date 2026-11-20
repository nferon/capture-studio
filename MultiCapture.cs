using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public partial class Studio {
 Studio captureHost,lastCaptureWindow;
 readonly List<Studio> captureChildren=new List<Studio>();
 static bool closingCaptureWindows;
 Studio CaptureHost {get{return captureHost??this;}}
 string CaptureDirectoryPath(){return String.IsNullOrWhiteSpace(CaptureHost.preferences.CaptureDirectory)?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),"Capture Studio"):CaptureHost.preferences.CaptureDirectory;}
 string EnsureCaptureDirectory(){string path=Path.GetFullPath(CaptureDirectoryPath());Directory.CreateDirectory(path);return path;}
 List<Studio> CaptureWindows(){var root=CaptureHost;return new[]{root}.Concat(root.captureChildren.Where(w=>!w.IsDisposed)).ToList();}
 protected virtual void OpenNewWindow(string[] args){var window=CreateCaptureWindow();window.HandleLaunch(args);}
 Studio CreateCaptureWindow(){
  var root=CaptureHost;var window=new Studio();window.captureHost=root;window.suppressRecovery=true;
  window.preferences=json.Deserialize<StudioPreferences>(json.Serialize(root.preferences));
  window.preferences.CloseToTray=false;
  if(root.persistenceEnabled)window.InitializePersistence(Path.Combine(root.dataDirectory,"fenetres",Guid.NewGuid().ToString("N")));
  window.captureMode.SelectedIndex=captureMode.SelectedIndex;window.delay.Value=delay.Value;
  window.StartPosition=FormStartPosition.Manual;window.Bounds=VisiblePlacement(new Rectangle(Left+32,Top+32,Width,Height),Screen.AllScreens.Select(s=>s.WorkingArea).ToArray());
  root.captureChildren.Add(window);window.FormClosed+=delegate{root.captureChildren.Remove(window);};
  window.ApplyWindowsTheme();return window;
 }
 List<Studio> PrepareCaptureWindows(int mode){return mode==1?new List<Studio>():HideCaptureWindows();}
 List<Studio> HideCaptureWindows(){var windows=CaptureWindows().Where(w=>w.Visible).ToList();foreach(var window in windows)window.Hide();return windows;}
 void RestoreCaptureWindows(List<Studio> windows){foreach(var window in windows)if(!window.IsDisposed)window.Show();}
 void AcceptCapture(Bitmap image,bool separate){
  if(!separate){SetPhoto(image);return;}
  Studio window=null;try{window=CreateCaptureWindow();window.SetPhoto(image);window.Show();window.status.Text="Nouvelle capture — les autres fenêtres sont conservées.";lastCaptureWindow=window;}
  catch{if(window!=null)window.Dispose();else image.Dispose();throw;}
 }
}
