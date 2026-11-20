using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

public static class SetupBridge {
 // 0: closed, 2: user cancelled, 3: legacy/unavailable, 4: operation in progress.
 public static int Main(){
  int session=Process.GetCurrentProcess().SessionId;bool running=false;
  foreach(var process in Process.GetProcessesByName("CaptureStudio"))using(process){
   try{if(process.SessionId!=session)continue;running=true;Version version;if(!Version.TryParse(FileVersionInfo.GetVersionInfo(process.MainModule.FileName).FileVersion,out version)||version<new Version(0,15,1,0))return 3;}catch{return 3;}
  }
  if(!running)return 0;
  string token=Guid.NewGuid().ToString("N"),request=SetupCloseProtocol.FileFor(token,"request"),reply=SetupCloseProtocol.FileFor(token,"reply");
  try{
   using(var marker=new FileStream(request,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){}
   using(var pipe=new NamedPipeClientStream(".",SetupCloseProtocol.PipeName,PipeDirection.Out)){
    pipe.Connect(3000);using(var writer=new StreamWriter(pipe)){writer.WriteLine("[\""+SetupCloseProtocol.Prefix+token+"\"]");writer.Flush();}
   }
   var elapsed=Stopwatch.StartNew();
   while(elapsed.Elapsed<TimeSpan.FromMinutes(5)){
    if(File.Exists(reply)){
     string status;try{status=File.ReadAllText(reply);}catch(IOException){Thread.Sleep(100);continue;}
     if(status=="cancelled")return 2;if(status=="busy")return 4;
     int id;if(status.StartsWith("ready:",StringComparison.Ordinal)&&Int32.TryParse(status.Substring(6),out id)){
      try{using(var process=Process.GetProcessById(id)){if(process.ProcessName!="CaptureStudio"||process.SessionId!=session)return 3;return process.WaitForExit(15000)?0:3;}}catch(ArgumentException){return 0;}catch(InvalidOperationException){return 0;}
     }
    }
    Thread.Sleep(100);
   }
   return 3;
  }catch{return 3;}
  finally{try{File.Delete(request);}catch{}try{File.Delete(reply);}catch{}}
 }
}
