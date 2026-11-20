using System;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Forms;
public static class VerifyUpdates {
 static void MustReject(Action action){try{action();}catch(System.IO.InvalidDataException){return;}throw new Exception("Publication invalide acceptee");}
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
  Console.WriteLine("PASS: version, depot epingle, prerelease/brouillon refuses, empreinte obligatoire, fichier altere refuse et limite de taille.");
 }
}
