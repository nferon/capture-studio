using System;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
public static class VerifyUpdates {
 static void MustReject(Action action){try{action();}catch(System.IO.InvalidDataException){return;}throw new Exception("Publication invalide acceptee");}
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Call(object obj,string method,params object[] args){return typeof(Studio).GetMethod(method,Flags).Invoke(obj,args);}
 static string WriteProject(string folder,string name,int width){using(var image=new Bitmap(width,40))using(var stream=new MemoryStream()){image.Save(stream,System.Drawing.Imaging.ImageFormat.Png);string path=Path.Combine(folder,name+".capture");File.WriteAllText(path,new JavaScriptSerializer().Serialize(new Project(){Image=Convert.ToBase64String(stream.ToArray()),Marks=new List<Mark>()}));return path;}}
 static void TestSession(){
  string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-update-session-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string first=WriteProject(folder,"one",80),second=WriteProject(folder,"two",100);
  using(var original=new Studio()){typeof(Studio).GetField("suppressRecovery",Flags).SetValue(original,true);Call(original,"InitializePersistence",folder);Call(original,"OpenExternalFile",first);using(var child=(Studio)Call(original,"CreateCaptureWindow")){Call(child,"OpenExternalFile",second);if(!(bool)Call(original,"PrepareUpdateRestart"))throw new Exception("Projets sauvegardes refuses");}}
  using(var restored=new Studio()){typeof(Studio).GetField("suppressRecovery",Flags).SetValue(restored,true);Call(restored,"InitializePersistence",folder);Call(restored,"RestoreUpdateSession");var windows=(IList)Call(restored,"CaptureWindows");if(windows.Count!=2)throw new Exception("Fenetres non reprises");foreach(Studio window in windows)if(window!=restored)window.Dispose();if(File.Exists(Path.Combine(folder,"resume-update.json"))||!File.Exists(first)||!File.Exists(second))throw new Exception("Fichiers de reprise incorrects");}
 }
 [STAThread] public static void Main(){
  byte[] bytes=Encoding.UTF8.GetBytes("Capture Studio update fixture");string digest;
  using(var sha=SHA256.Create())digest="sha256:"+BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
  var asset=new StudioReleaseAsset(){name="CaptureStudio-Setup-0.16.0.exe",browser_download_url="https://github.com/nferon/capture-studio/releases/download/v0.16.0/CaptureStudio-Setup-0.16.0.exe",digest=digest,size=bytes.Length};
  var release=new StudioRelease(){tag_name="v0.16.0",assets=new[]{asset}};
  if(StudioUpdate.Installer(release)!=asset||!StudioUpdate.MatchesHash(bytes,digest))throw new Exception("Publication valide refusee");
  if(StudioUpdate.MatchesHash(Encoding.UTF8.GetBytes("changed"),digest))throw new Exception("Fichier altere accepte");
  asset.browser_download_url="https://example.com/installer.exe";MustReject(()=>StudioUpdate.Installer(release));
  asset.browser_download_url="https://github.com/nferon/capture-studio/releases/download/v0.16.0/CaptureStudio-Setup-0.16.0.exe";
  release.prerelease=true;MustReject(()=>StudioUpdate.Installer(release));release.prerelease=false;
  release.draft=true;MustReject(()=>StudioUpdate.Installer(release));release.draft=false;
  asset.digest=null;MustReject(()=>StudioUpdate.Installer(release));asset.digest=digest;
  asset.size=200L*1024*1024;MustReject(()=>StudioUpdate.Installer(release));
  MustReject(()=>StudioUpdate.VersionFromTag("v1.0.0-preview"));
  MustReject(()=>StudioUpdate.VersionFromTag("../../installer.exe"));
  using(var form=new Studio()){if(!form.Text.Contains(Studio.ReleaseVersion))throw new Exception("Version UI incorrecte");}
  TestSession();
  Console.WriteLine("PASS: version, depot epingle, prerelease/brouillon refuses, empreinte obligatoire, fichier altere refuse et limite de taille.");
 }
}
