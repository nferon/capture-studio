using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

public static class SetupCloseProtocol {
 public const string Prefix="--prepare-installation=";
 public static string PipeName {get{return "CaptureStudio."+WindowsIdentity.GetCurrent().User.Value+"."+Process.GetCurrentProcess().SessionId;}}
 public static bool ValidToken(string token){Guid value;return token!=null&&Guid.TryParseExact(token,"N",out value);}
 public static string FileFor(string token,string kind){if(!ValidToken(token)||(kind!="request"&&kind!="reply"))throw new ArgumentException("Demande d’installation non valide.");return Path.Combine(Path.GetTempPath(),"CaptureStudio-Install-"+token+"."+kind);}
}
