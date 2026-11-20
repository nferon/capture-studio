using System;
public static class VerifyLiveUpdates {
 public static void Main(){
  var release=StudioUpdate.Latest().GetAwaiter().GetResult();
  var asset=StudioUpdate.Installer(release);
  byte[] bytes=StudioUpdate.Download(asset.browser_download_url,150*1024*1024).GetAwaiter().GetResult();
  if(bytes.LongLength!=asset.size||!StudioUpdate.MatchesHash(bytes,asset.digest))throw new Exception("Empreinte de la version publique incorrecte");
  Console.WriteLine("PASS: API publique, version "+release.tag_name+", installateur telecharge ("+bytes.Length+" octets), SHA-256 GitHub valide. Aucun installateur execute.");
 }
}
