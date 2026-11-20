using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
sealed class OverlayProbe:Form {protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000020;return p;}}}
public static class VerifyCaptureDelay {
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Field(object source,string name){return source.GetType().GetField(name,Private).GetValue(source);}
 [STAThread] public static void Main(){
  using(var root=new Studio())using(var overlay=new OverlayProbe()){
   root.Opacity=0;root.ShowInTaskbar=false;root.Show();Application.DoEvents();
   var child=(Studio)typeof(Studio).GetMethod("CreateCaptureWindow",Private).Invoke(root,null);
   using(child){
    child.Opacity=0;child.ShowInTaskbar=false;child.Show();Application.DoEvents();
    ((NumericUpDown)Field(root,"delay")).Value=0;((NumericUpDown)Field(child,"delay")).Value=3;
    typeof(Studio).GetMethod("SetPhoto",Private).Invoke(child,new object[]{new Bitmap(80,60)});
    var rootButton=(Button)Field(root,"captureAction");var childButton=(Button)Field(child,"captureAction");var share=(Button)Field(child,"shareAction");
    string rootText=rootButton.Text,childText=childButton.Text;
    var elapsed=Stopwatch.StartNew();var task=(Task)typeof(Studio).GetMethod("WaitCaptureDelayAsync",Private).Invoke(child,new object[]{(int)((NumericUpDown)Field(child,"delay")).Value});
    if(childButton.Text!="3 s…"||childButton.Enabled||!share.Enabled||rootButton.Text!=rootText)throw new Exception("Le retardateur bloque Partager ou utilise la mauvaise fenetre");
    while(!task.IsCompleted&&elapsed.Elapsed.TotalSeconds<6){Application.DoEvents();Thread.Sleep(10);}task.GetAwaiter().GetResult();
    if(elapsed.Elapsed.TotalSeconds<2.9||childButton.Text!=childText||!childButton.Enabled)throw new Exception("Delai de 3 s ou restauration incorrect");
    overlay.Text="Surcouche transparente de test";overlay.Opacity=0;overlay.ShowInTaskbar=false;overlay.Show();Application.DoEvents();
    var windows=WindowCapture.List(IntPtr.Zero);
    if(windows.Any(w=>w.Handle==overlay.Handle)||!windows.Any(w=>w.Handle==child.Handle))throw new Exception("Filtrage de surcouche incorrect");
    if(WindowCapture.IsSelectableStyle(0x20)||WindowCapture.IsSelectableStyle(0x08000000)||!WindowCapture.IsSelectableStyle(0x80000))throw new Exception("Styles de fenetre incorrects");
    Console.WriteLine("PASS: seconde fenetre 3 s / premiere 0 s ; temps mesure "+elapsed.Elapsed.TotalSeconds.ToString("0.00")+" s ; Partager actif, texte restaure, surcouche transparente exclue, editeur selectionnable.");
   }
  }
 }
}
