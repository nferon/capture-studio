using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
public static class VerifyOcrSelection {
 [STAThread] public static void Main(){
  var assembly=typeof(Studio).Assembly;var viewerType=assembly.GetType("OcrSelectionCanvas");var flags=BindingFlags.Static|BindingFlags.Public;
  var display=(RectangleF)viewerType.GetMethod("ImageDisplayBounds",flags).Invoke(null,new object[]{new Size(900,300),new Size(450,240)});
  var center=(PointF)viewerType.GetMethod("ImagePosition",flags).Invoke(null,new object[]{new Point(225,120),new Size(900,300),display});
  if(Math.Abs(center.X-450)>1||Math.Abs(center.Y-150)>1)throw new Exception("Position dans une image ajustee incorrecte");
  var bounds=(Rectangle)viewerType.GetMethod("SelectionBounds",flags).Invoke(null,new object[]{new PointF(750,130),new PointF(20,20),new Size(900,300)});
  if(bounds!=new Rectangle(20,20,730,110))throw new Exception("Selection inversee incorrecte");
  var clamped=(Rectangle)viewerType.GetMethod("SelectionBounds",flags).Invoke(null,new object[]{new PointF(-20,-20),new PointF(1000,400),new Size(900,300)});
  if(clamped!=new Rectangle(0,0,900,300))throw new Exception("Selection hors image incorrecte");
  using(var source=new Bitmap(900,340))using(var studio=new Studio()){
   using(var g=Graphics.FromImage(source))using(var font=new Font("Arial",30,FontStyle.Bold)){g.Clear(Color.White);g.DrawString("ZONE ALPHA 123",font,Brushes.Black,40,35);g.DrawString("OTHER BETA 987",font,Brushes.Black,40,230);}
   var picker=(Form)Activator.CreateInstance(assembly.GetType("OcrImagePicker"),new object[]{source,studio.Icon});
   using(picker){picker.ShowInTaskbar=false;picker.Opacity=0;picker.Show();Application.DoEvents();using(var preview=new Bitmap(picker.Width,picker.Height)){picker.DrawToBitmap(preview,new Rectangle(0,0,preview.Width,preview.Height));preview.Save("verification-selection-ocr.png");}picker.Close();}
   using(var region=source.Clone(new Rectangle(0,0,900,150),System.Drawing.Imaging.PixelFormat.Format32bppArgb)){
    var task=(Task<string>)typeof(Studio).GetMethod("ExtractOcrText",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(studio,new object[]{region});
    var deadline=DateTime.UtcNow.AddSeconds(40);while(!task.IsCompleted&&DateTime.UtcNow<deadline){Application.DoEvents();Thread.Sleep(20);}if(!task.IsCompleted)throw new Exception("Delai OCR depasse");
    string text=task.GetAwaiter().GetResult();if(text.IndexOf("ALPHA",StringComparison.OrdinalIgnoreCase)<0||text.IndexOf("BETA",StringComparison.OrdinalIgnoreCase)>=0)throw new Exception("Mauvaise zone OCR : "+text);
    Console.WriteLine("PASS: image ajustee, rectangle inverse, limites, fenetre de selection et OCR Windows limite a la zone : "+text.Trim());
   }
  }
 }
}
