using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
public static class VerifyClearCapture {
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Get(object obj,string name){return typeof(Studio).GetField(name,Flags).GetValue(obj);}
 static object Call(object obj,string name,params object[] args){return typeof(Studio).GetMethod(name,Flags).Invoke(obj,args);}
 [STAThread] public static void Main(){
  string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-clear-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  using(var main=new Studio()){
   typeof(Studio).GetField("suppressRecovery",Flags).SetValue(main,true);Call(main,"InitializePersistence",folder);
   var original=new Bitmap(120,80);original.SetPixel(10,10,Color.Red);string saved=Path.Combine(folder,"original.png");original.Save(saved);byte[] before=File.ReadAllBytes(saved);
   Call(main,"SetPhoto",original);((IList)Get(main,"marks")).Add(new Mark(){Kind="Texte",Text="annotation"});Call(main,"SaveDraft");if(Call(main,"ReadDraft")==null)throw new Exception("Brouillon de test absent");
   using(var other=(Studio)Call(main,"CreateCaptureWindow")){
    Call(other,"SetPhoto",new Bitmap(40,30));Call(main,"ClearCapture");
    if(Get(main,"photo")!=null||((IList)Get(main,"marks")).Count!=0||Get(main,"projectPath")!=null)throw new Exception("Editeur non vide");
    if(((Button)Get(main,"shareAction")).Enabled||((Button)Get(main,"copyAction")).Enabled)throw new Exception("Actions actives sans image");
    if(Call(main,"ReadDraft")!=null)throw new Exception("Ancienne capture restauree apres effacement");
    if(((Bitmap)Get(other,"photo")).Width!=40)throw new Exception("Autre editeur modifie");
    byte[] after=File.ReadAllBytes(saved);if(Convert.ToBase64String(before)!=Convert.ToBase64String(after))throw new Exception("Fichier enregistre modifie");
    Call(main,"SetPhoto",new Bitmap(60,50));Call(main,"SaveDraft");if(Call(main,"ReadDraft")==null)throw new Exception("Nouveau brouillon non sauvegarde");
   }
  }
  using(var bitmap=new Bitmap(800,600))using(var picker=new WindowPicker(bitmap,new Rectangle(0,0,800,600),new List<CaptureWindow>(),3)){
   if(!picker.TimingInstruction.Contains("3 secondes")||!picker.TimingInstruction.Contains("APRÈS"))throw new Exception("Instruction du retardateur incorrecte");
   picker.ShowInTaskbar=false;picker.Opacity=0;picker.Show();Application.DoEvents();using(var preview=new Bitmap(800,600)){picker.DrawToBitmap(preview,new Rectangle(0,0,800,600));preview.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"instruction-retardateur.png"));}picker.Close();
  }
  Console.WriteLine("PASS: editeur vide, annotations retirees, autre fenetre et fichier preserves, ancien brouillon non restaure, nouvelle capture possible, instruction selection puis delai.");
 }
}
