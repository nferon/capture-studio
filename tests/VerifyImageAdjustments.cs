using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
public static class VerifyImageAdjustments {
 [STAThread] public static void Main(){
  using(var blank=new Bitmap(320,240)){using(var g=Graphics.FromImage(blank))g.Clear(Color.White);if(ImageProcessing.DetectRectangle(blank).Found)throw new Exception("Faux contour sur image vide");}
  using(var image=new Bitmap(900,700)){
   var q=new[]{new PointF(300,70),new PointF(460,100),new PointF(560,620),new PointF(230,580)};
   using(var g=Graphics.FromImage(image)){g.Clear(Color.FromArgb(215,200,176));g.SmoothingMode=SmoothingMode.AntiAlias;g.FillPolygon(Brushes.Black,q);var inner=new[]{new PointF(310,130),new PointF(452,154),new PointF(530,550),new PointF(260,520)};g.FillPolygon(Brushes.SteelBlue,inner);using(var path=new GraphicsPath()){path.AddPolygon(inner);g.SetClip(path);using(var font=new Font("Arial",22,FontStyle.Bold))g.DrawString("LOCAL TEST",font,Brushes.White,300,310);g.ResetClip();}}
   var elapsed=Stopwatch.StartNew();var detection=ImageProcessing.DetectRectangle(image);
   if(!detection.Found)throw new Exception("Telephone synthetique non detecte");
   for(int i=0;i<4;i++){double distance=Math.Sqrt(Math.Pow(detection.Corners[i].X-q[i].X,2)+Math.Pow(detection.Corners[i].Y-q[i].Y,2));if(distance>25)throw new Exception("Coin detecte trop eloigne: "+i+" distance "+distance);}
   if(!ImageProcessing.NeedsStraightening(detection))throw new Exception("Perspective inclinee non proposee");
   var options=new ImageAdjustOptions(){Corners=detection.Corners,Background=ImageProcessing.AutoBackground(image)};
   using(var corrected=ImageProcessing.Render(image,options,1600)){corrected.Save("verification-redressement.png");if(corrected.Width<=100||corrected.Height<=300)throw new Exception("Dimensions corrigees incorrectes");}
   using(var original=new Bitmap(image))using(var studio=new Studio()){
    var dialog=(Form)Activator.CreateInstance(typeof(Studio).Assembly.GetType("ImageAdjustmentsDialog"),BindingFlags.Instance|BindingFlags.Public,null,new object[]{image,studio.Icon,detection},null);
    using(dialog){dialog.ShowInTaskbar=false;dialog.Opacity=0;dialog.Show();Application.DoEvents();using(var preview=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(preview,new Rectangle(0,0,preview.Width,preview.Height));preview.Save("verification-redressement-interface.png");}dialog.Close();}
    if(image.GetPixel(300,70)!=original.GetPixel(300,70))throw new Exception("Image source modifiee");
   }
   var theme=typeof(Studio).Assembly.GetType("StudioTheme").GetField("Dark",BindingFlags.Static|BindingFlags.Public);theme.SetValue(null,true);
   try{using(var studio=new Studio())using(var darkDialog=(Form)Activator.CreateInstance(typeof(Studio).Assembly.GetType("ImageAdjustmentsDialog"),BindingFlags.Instance|BindingFlags.Public,null,new object[]{image,studio.Icon,detection},null)){
    darkDialog.Size=new Size(1000,700);darkDialog.ShowInTaskbar=false;darkDialog.Opacity=0;darkDialog.Show();Application.DoEvents();using(var preview=new Bitmap(darkDialog.Width,darkDialog.Height)){darkDialog.DrawToBitmap(preview,new Rectangle(0,0,preview.Width,preview.Height));preview.Save("verification-redressement-sombre.png");}darkDialog.Close();
   }}finally{theme.SetValue(null,false);}
   Console.WriteLine("Detection: confiance "+detection.Confidence.ToString("0.00")+", temps "+elapsed.Elapsed.TotalSeconds.ToString("0.00")+" s");
  }
  using(var test=new Bitmap(80,120)){
   using(var g=Graphics.FromImage(test))g.Clear(Color.FromArgb(80,100,120));test.SetPixel(0,0,Color.Red);test.SetPixel(79,119,Color.Blue);
   using(var identity=ImageProcessing.Perspective(test,ImageProcessing.FullImage(test.Size),0,1000)){if(identity.Size!=test.Size||identity.GetPixel(0,0).R<250||identity.GetPixel(79,119).B<250)throw new Exception("Perspective identite incorrecte");}
   var crossed=new[]{new PointF(0,0),new PointF(79,119),new PointF(79,0),new PointF(0,119)};if(ImageProcessing.ValidCorners(crossed,test.Size))throw new Exception("Coins croises acceptes");
   var options=new ImageAdjustOptions(){Corners=ImageProcessing.FullImage(test.Size),BackgroundMode=1,Rotation=90};using(var rotated=ImageProcessing.Render(test,options,1000)){if(rotated.Width!=120||rotated.Height!=80)throw new Exception("Rotation 90 dimensions incorrectes: "+rotated.Size);}
   options.Rotation=0;options.Brightness=25;using(var lighter=ImageProcessing.Render(test,options,1000)){if(lighter.GetPixel(40,60).R<=test.GetPixel(40,60).R)throw new Exception("Luminosite non appliquee");}
   options.Brightness=0;options.Saturation=-100;using(var gray=ImageProcessing.Render(test,options,1000)){var p=gray.GetPixel(40,60);if(p.R!=p.G||p.G!=p.B)throw new Exception("Saturation non appliquee");}
   options.Saturation=0;options.BackgroundMode=2;options.Background=Color.Magenta;options.Padding=10;options.Shadow=false;using(var background=ImageProcessing.Render(test,options,1000)){if(background.GetPixel(0,0).ToArgb()!=Color.Magenta.ToArgb())throw new Exception("Fond non applique");}
  }
  using(var dark=new Bitmap(160,100)){
   for(int y=0;y<dark.Height;y++)for(int x=0;x<dark.Width;x++){int value=40+x*50/dark.Width;dark.SetPixel(x,y,Color.FromArgb(value,value,value));}
   var tuning=ImageProcessing.AutoLight(dark);if(tuning[0]<=0)throw new Exception("Amelioration automatique assombrit une photo sombre");
  }
  using(var transparent=new Bitmap(20,30)){using(var g=Graphics.FromImage(transparent))g.Clear(Color.Transparent);transparent.SetPixel(10,10,Color.Red);using(var copy=ImageProcessing.Perspective(transparent,ImageProcessing.FullImage(transparent.Size),0,100)){if(copy.GetPixel(0,0).A!=0||copy.GetPixel(10,10).A!=255)throw new Exception("Transparence perdue");}}
  Console.WriteLine("PASS: detection locale d un rectangle incline, perspective, rotation, luminosite, saturation, fond, rejet des coins croises et source preservee.");
 }
}
