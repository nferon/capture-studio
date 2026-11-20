using System;
using System.IO;
using System.Reflection;
public class ShutdownProbe:Studio {
 public bool Accepted,ExpireRequest;
 public string Request;
 public int Prepared,ShutdownCount;
 protected override bool PrepareUpdateRestart(){Prepared++;if(ExpireRequest)File.Delete(Request);return Accepted;}
 protected override void CompleteInstallerShutdown(){ShutdownCount++;}
}
public static class VerifyInstallerShutdown {
 [STAThread] public static void Main(){
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;var handler=typeof(Studio).GetMethod("HandleInstallerCloseRequest",flags);
  using(var form=new ShutdownProbe()){
   string token=Guid.NewGuid().ToString("N"),request=SetupCloseProtocol.FileFor(token,"request"),reply=SetupCloseProtocol.FileFor(token,"reply");form.Request=request;
   try{
    handler.Invoke(form,new object[]{SetupCloseProtocol.Prefix+token});if(form.Prepared!=0)throw new Exception("Demande expiree acceptee");
    File.WriteAllText(request,"");handler.Invoke(form,new object[]{SetupCloseProtocol.Prefix+token});if(File.ReadAllText(reply)!="cancelled"||form.ShutdownCount!=0)throw new Exception("Annulation non respectee");File.Delete(reply);
    form.Accepted=true;form.ExpireRequest=true;handler.Invoke(form,new object[]{SetupCloseProtocol.Prefix+token});if(form.ShutdownCount!=0||File.Exists(reply))throw new Exception("Fermeture apres expiration");
    form.ExpireRequest=false;File.WriteAllText(request,"");typeof(Studio).GetField("capturing",flags).SetValue(form,true);handler.Invoke(form,new object[]{SetupCloseProtocol.Prefix+token});if(File.ReadAllText(reply)!="busy"||form.ShutdownCount!=0)throw new Exception("Capture en cours interrompue");File.Delete(reply);typeof(Studio).GetField("capturing",flags).SetValue(form,false);
    handler.Invoke(form,new object[]{SetupCloseProtocol.Prefix+token});if(!File.ReadAllText(reply).StartsWith("ready:")||form.ShutdownCount!=1)throw new Exception("Fermeture autorisee non demandee");
    handler.Invoke(form,new object[]{SetupCloseProtocol.Prefix+"../../private"});if(form.ShutdownCount!=1)throw new Exception("Jeton invalide accepte");
   }finally{File.Delete(request);File.Delete(reply);}
  }
  Console.WriteLine("PASS: demande expiree refusee, sauvegarde annulee respectee, capture en cours preservee, fermeture uniquement apres accord et jeton valide.");
 }
}
