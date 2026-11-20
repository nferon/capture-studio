using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

public partial class Studio {
 void ClearCapture(){
  if(photo==null)return;
  if(CaptureHost.capturing||CaptureHost.ocrBusy){status.Text="Attendez la fin de la capture ou de l’OCR avant de vider l’image.";return;}
  FinishInlineText(true);if(!ConfirmDiscard())return;
  try{
   // A deliberate empty document must not restore the previous autosaved image.
   if(persistenceEnabled){string empty=json.Serialize(new Project(){Empty=true,Marks=new List<Mark>()});AtomicWrite(Path.Combine(dataDirectory,"brouillon.capture"),empty);lastDraft=empty;}
   EndPan();dragging=false;cropDragging=false;spaceHeld=false;SetPhoto(null);
   status.Text="Fenêtre vide — prête pour une nouvelle capture ou pour ouvrir une image.";
  }catch(Exception ex){MessageBox.Show(this,ex.Message,"Impossible de vider la capture");}
 }
}
