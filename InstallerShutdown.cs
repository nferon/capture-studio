using System;
using System.Diagnostics;
using System.IO;

public partial class Studio {
 protected virtual void CompleteInstallerShutdown(){QuitApplication();}
 void HandleInstallerCloseRequest(string argument){
  if(argument==null||!argument.StartsWith(SetupCloseProtocol.Prefix,StringComparison.Ordinal))return;
  string token=argument.Substring(SetupCloseProtocol.Prefix.Length);if(!SetupCloseProtocol.ValidToken(token))return;
  string request=SetupCloseProtocol.FileFor(token,"request"),reply=SetupCloseProtocol.FileFor(token,"reply");
  if(!File.Exists(request))return;
  try{
   if(!Enabled||CaptureHost.capturing||CaptureHost.ocrBusy){File.WriteAllText(reply,"busy");return;}
   if(!PrepareUpdateRestart()){File.WriteAllText(reply,"cancelled");return;}
   if(!File.Exists(request))return;
   File.WriteAllText(reply,"ready:"+Process.GetCurrentProcess().Id);
   CompleteInstallerShutdown();
  }catch{try{File.WriteAllText(reply,"cancelled");}catch{}}
 }
}
