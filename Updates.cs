using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;

public class StudioReleaseAsset {public string name,browser_download_url,digest;public long size;}
public class StudioRelease {public string tag_name,html_url;public bool draft,prerelease;public StudioReleaseAsset[] assets;}
public static class StudioUpdate {
 public const string Repository="nferon/capture-studio";
 public const string LatestApi="https://api.github.com/repos/"+Repository+"/releases/latest";
 public static Version VersionFromTag(string tag){Version version;if(tag==null||!Regex.IsMatch(tag,@"^v\d+\.\d+\.\d+$")||!Version.TryParse(tag.Substring(1),out version))throw new InvalidDataException("Numéro de version non valide.");return version;}
 public static StudioReleaseAsset Installer(StudioRelease release){
  if(release==null||release.draft||release.prerelease)throw new InvalidDataException("La version n’est pas une publication stable.");
  string version=VersionFromTag(release.tag_name).ToString(3),name="CaptureStudio-Setup-"+version+".exe";
  var asset=(release.assets??new StudioReleaseAsset[0]).SingleOrDefault(a=>a.name==name);
  string expected="https://github.com/"+Repository+"/releases/download/"+release.tag_name+"/"+name;
  if(asset==null||asset.browser_download_url!=expected||asset.size<1||asset.size>150*1024*1024||!Regex.IsMatch(asset.digest??"",@"^sha256:[0-9a-fA-F]{64}$"))throw new InvalidDataException("L’installateur ou son empreinte de vérification est indisponible.");
  return asset;
 }
 public static bool MatchesHash(byte[] bytes,string digest){using(var hash=SHA256.Create()){string actual="sha256:"+BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();return String.Equals(actual,digest,StringComparison.OrdinalIgnoreCase);}}
 public static async Task<byte[]> Download(string url,int limit){
  ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
  using(var client=new HttpClient()){client.Timeout=TimeSpan.FromSeconds(90);client.DefaultRequestHeaders.UserAgent.ParseAdd("CaptureStudio/"+Studio.ReleaseVersion);client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
   using(var response=await client.GetAsync(url,HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false)){
    response.EnsureSuccessStatusCode();var uri=response.RequestMessage.RequestUri;
    if(uri.Scheme!="https"||!(new[]{"api.github.com","github.com","release-assets.githubusercontent.com","objects.githubusercontent.com"}).Contains(uri.Host))throw new InvalidDataException("Adresse de téléchargement non autorisée.");
    if(response.Content.Headers.ContentLength>limit)throw new InvalidDataException("Téléchargement trop volumineux.");
    using(var stream=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))using(var memory=new MemoryStream()){
     byte[] buffer=new byte[65536];int read;while((read=await stream.ReadAsync(buffer,0,buffer.Length).ConfigureAwait(false))>0){if(memory.Length+read>limit)throw new InvalidDataException("Téléchargement trop volumineux.");memory.Write(buffer,0,read);}return memory.ToArray();
    }
   }
  }
 }
 public static async Task<StudioRelease> Latest(){byte[] data=await Download(LatestApi,1024*1024).ConfigureAwait(false);var release=new JavaScriptSerializer().Deserialize<StudioRelease>(Encoding.UTF8.GetString(data));VersionFromTag(release.tag_name);return release;}
}

public partial class Studio {
 bool checkingUpdates;
 async Task CheckUpdates(bool interactive){
  if(captureHost!=null){await captureHost.CheckUpdates(interactive);return;}
  if(checkingUpdates)return;checkingUpdates=true;
  try{
   if(interactive)status.Text="Recherche d’une mise à jour…";
   var release=await StudioUpdate.Latest();if(IsDisposed)return;
   var version=StudioUpdate.VersionFromTag(release.tag_name);
   if(version<=new Version(ReleaseVersion)){if(interactive)MessageBox.Show(this,"Capture Studio "+ReleaseVersion+" est à jour.","Mises à jour");return;}
   status.Text="Capture Studio "+version.ToString(3)+" est disponible — Fichier > Rechercher les mises à jour.";
   if(!interactive)return;
   var asset=StudioUpdate.Installer(release);
   if(MessageBox.Show(this,"La version "+version.ToString(3)+" est disponible. Télécharger puis installer la mise à jour ?\n\nVos captures ouvertes devront être enregistrées avant le redémarrage.","Mise à jour de Capture Studio",MessageBoxButtons.YesNo,MessageBoxIcon.Information)!=DialogResult.Yes)return;
   status.Text="Téléchargement de la version "+version.ToString(3)+"…";
   byte[] bytes=await StudioUpdate.Download(asset.browser_download_url,150*1024*1024);if(IsDisposed)return;
   if(bytes.LongLength!=asset.size||!StudioUpdate.MatchesHash(bytes,asset.digest))throw new InvalidDataException("L’empreinte du téléchargement ne correspond pas. L’installation est annulée.");
   string cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CaptureStudio","updates",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(cache);
   string installer=Path.Combine(cache,asset.name);File.WriteAllBytes(installer,bytes);
   if(!PrepareUpdateRestart())return;
   using(var process=Process.Start(new ProcessStartInfo(installer,"/SP- /SILENT /NORESTART /WAITPID="+Process.GetCurrentProcess().Id+" /RESTARTAPP=1"){UseShellExecute=true})){}
   QuitApplication();
  }catch(HttpRequestException){if(interactive&&!IsDisposed)MessageBox.Show(this,"Impossible de joindre GitHub. Vous pouvez réessayer plus tard.","Mises à jour");}
  catch(Exception ex){if(interactive&&!IsDisposed)MessageBox.Show(this,ex.Message,"Mise à jour impossible");}
  finally{checkingUpdates=false;}
 }
 bool PrepareUpdateRestart(){
  if(CaptureHost.capturing||CaptureHost.ocrBusy){MessageBox.Show(this,"Attendez la fin de la capture ou de l’OCR avant d’installer la mise à jour.","Mises à jour");return false;}
  foreach(var window in CaptureWindows()){
   if(window.IsDisposed)continue;window.FinishInlineText(true);
   if(window.photo!=null&&(window.dirty||window.projectPath==null)){
    window.Show();window.Activate();
    if(MessageBox.Show(window,"Enregistrez cette capture en projet avant le redémarrage. Annuler conserve toutes les fenêtres ouvertes.","Préparer la mise à jour",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return false;
    window.SaveProject();if(window.dirty||window.projectPath==null)return false;
   }
   window.SaveDraft();window.SavePreferences();
  }
  if(CaptureWindows().Any(w=>w.photo!=null&&(w.dirty||w.projectPath==null)))return false;
  if(persistenceEnabled)AtomicWrite(Path.Combine(dataDirectory,"resume-update.json"),json.Serialize(CaptureWindows().Where(w=>w.photo!=null&&w.projectPath!=null).Select(w=>w.projectPath).ToArray()));
  return true;
 }
 void RestoreUpdateSession(){
  if(!persistenceEnabled)return;string path=Path.Combine(dataDirectory,"resume-update.json");if(!File.Exists(path))return;
  try{if(new FileInfo(path).Length>1024*1024)throw new InvalidDataException("Session de reprise trop volumineuse.");var files=json.Deserialize<string[]>(File.ReadAllText(path))??new string[0];if(files.Length>1000)throw new InvalidDataException("Trop de projets à reprendre.");bool first=true;foreach(string file in files){if(String.IsNullOrWhiteSpace(file)||!file.EndsWith(".capture",StringComparison.OrdinalIgnoreCase)||!File.Exists(file))continue;if(first){OpenExternalFile(file);first=false;}else OpenNewWindow(new[]{file});}File.Delete(path);}
  catch(Exception ex){MessageBox.Show(this,"Les projets restent enregistrés et peuvent être ouverts avec Ctrl+O.\n\n"+ex.Message,"Reprise après mise à jour");}
 }
 async void CheckStartupUpdate(){
  if(!preferences.CheckUpdates)return;
  DateTime last;if(DateTime.TryParse(preferences.LastUpdateCheckUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out last)&&DateTime.UtcNow-last.ToUniversalTime()<TimeSpan.FromDays(1))return;
  preferences.LastUpdateCheckUtc=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture);SavePreferences();await CheckUpdates(false);
 }
}
