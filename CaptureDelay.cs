using System;
using System.Diagnostics;
using System.Threading.Tasks;

public partial class Studio {
 async Task WaitCaptureDelayAsync(int seconds){
  if(seconds<=0)return;
  string oldText=captureAction.Text,oldStatus=status.Text;bool wasEnabled=captureAction.Enabled;
  var elapsed=Stopwatch.StartNew();int last=-1;
  try{
   captureAction.Enabled=false;
   while(elapsed.Elapsed.TotalSeconds<seconds){
    if(IsDisposed)throw new OperationCanceledException("La fenêtre de capture a été fermée.");
    int remaining=(int)Math.Ceiling(seconds-elapsed.Elapsed.TotalSeconds);
    if(remaining!=last){last=remaining;captureAction.Text=remaining+" s…";status.Text="Capture dans "+remaining+" s — vous pouvez ouvrir le menu à montrer.";}
    await Task.Delay(Math.Max(1,Math.Min(100,(int)Math.Ceiling(seconds*1000-elapsed.Elapsed.TotalMilliseconds))));
   }
  }finally{
   if(!IsDisposed){captureAction.Text=oldText;captureAction.Enabled=wasEnabled;status.Text=oldStatus;captureAction.Update();status.Update();}
  }
 }
}
