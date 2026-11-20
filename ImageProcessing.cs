using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

public sealed class RectangleProposal {public PointF[] Corners;public double Confidence;public bool Found;}
public sealed class ImageAdjustOptions {
 public PointF[] Corners;public float Rotation,Brightness,Contrast,Saturation;public float Aspect;
 public int BackgroundMode,Padding=8;public bool Shadow=true;public Color Background=Color.White;
 public ImageAdjustOptions Copy(){var result=(ImageAdjustOptions)MemberwiseClone();result.Corners=(PointF[])Corners.Clone();return result;}
}
public static class ImageProcessing {
 public static PointF[] FullImage(Size size){return new[]{new PointF(0,0),new PointF(size.Width-1,0),new PointF(size.Width-1,size.Height-1),new PointF(0,size.Height-1)};}
 static double Cross(PointF a,PointF b,PointF c){return (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);}
 static double Distance(PointF a,PointF b){return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
 static double Area(PointF[] points){double a=0;for(int i=0;i<points.Length;i++){var p=points[i];var q=points[(i+1)%points.Length];a+=p.X*q.Y-p.Y*q.X;}return Math.Abs(a)/2;}
 public static bool ValidCorners(PointF[] points,Size source){
  if(points==null||points.Length!=4)return false;double sign=0;
  for(int i=0;i<4;i++){var p=points[i];if(float.IsNaN(p.X)||float.IsNaN(p.Y)||float.IsInfinity(p.X)||float.IsInfinity(p.Y)||p.X<0||p.Y<0||p.X>source.Width-1||p.Y>source.Height-1)return false;double c=Cross(p,points[(i+1)%4],points[(i+2)%4]);if(Math.Abs(c)<.01)return false;if(sign==0)sign=c;else if(sign*c<=0)return false;}
  return Area(points)>=25;
 }
 static List<PointF> Hull(List<PointF> points){
  points=points.OrderBy(p=>p.X).ThenBy(p=>p.Y).ToList();var hull=new List<PointF>();
  foreach(var point in points){while(hull.Count>=2&&Cross(hull[hull.Count-2],hull[hull.Count-1],point)<=0)hull.RemoveAt(hull.Count-1);hull.Add(point);}int lower=hull.Count;
  for(int i=points.Count-2;i>=0;i--){var point=points[i];while(hull.Count>lower&&Cross(hull[hull.Count-2],hull[hull.Count-1],point)<=0)hull.RemoveAt(hull.Count-1);hull.Add(point);}if(hull.Count>1)hull.RemoveAt(hull.Count-1);return hull;
 }
 static PointF[] Quad(List<PointF> hull){
  var q=new List<PointF>(hull);while(q.Count>4){int at=0;double cost=Double.MaxValue;for(int i=0;i<q.Count;i++){double area=Math.Abs(Cross(q[(i+q.Count-1)%q.Count],q[i],q[(i+1)%q.Count]));if(area<cost){cost=area;at=i;}}q.RemoveAt(at);}
  if(q.Count!=4)return null;int start=0;for(int i=1;i<4;i++)if(q[i].X+q[i].Y<q[start].X+q[start].Y)start=i;
  var ordered=new PointF[4];for(int i=0;i<4;i++)ordered[i]=q[(i+start)%4];if(Cross(ordered[0],ordered[1],ordered[2])<0)ordered=new[]{ordered[0],ordered[3],ordered[2],ordered[1]};return ordered;
 }
 public static RectangleProposal DetectRectangle(Bitmap original){
  double ratio=Math.Min(1,640.0/Math.Max(original.Width,original.Height));int w=Math.Max(3,(int)(original.Width*ratio)),h=Math.Max(3,(int)(original.Height*ratio));
  using(var small=new Bitmap(w,h,PixelFormat.Format32bppArgb)){
   using(var g=Graphics.FromImage(small)){g.Clear(Color.White);g.DrawImage(original,new Rectangle(0,0,w,h));}
   byte[] pixels=ReadPixels(small);var gray=new int[w*h];for(int i=0;i<gray.Length;i++)gray[i]=(pixels[i*4+2]*77+pixels[i*4+1]*150+pixels[i*4]*29)>>8;
   var magnitude=new int[w*h];var histogram=new int[2041];int edges=0;
   for(int y=1;y<h-1;y++)for(int x=1;x<w-1;x++){int i=y*w+x;int gx=-gray[i-w-1]+gray[i-w+1]-2*gray[i-1]+2*gray[i+1]-gray[i+w-1]+gray[i+w+1];int gy=-gray[i-w-1]-2*gray[i-w]-gray[i-w+1]+gray[i+w-1]+2*gray[i+w]+gray[i+w+1];int value=Math.Abs(gx)+Math.Abs(gy);magnitude[i]=value;if(value>=50){histogram[value]++;edges++;}}
   var fallback=new RectangleProposal(){Corners=FullImage(original.Size)};if(edges<20)return fallback;
   int threshold=50,total=0;for(int i=50;i<histogram.Length;i++){total+=histogram[i];if(total>=edges*.65){threshold=i;break;}}
   var edge=new bool[w*h];for(int y=1;y<h-1;y++)for(int x=1;x<w-1;x++)if(magnitude[y*w+x]>=threshold)for(int yy=-1;yy<=1;yy++)for(int xx=-1;xx<=1;xx++)edge[(y+yy)*w+x+xx]=true;
   var seen=new bool[edge.Length];var queue=new int[edge.Length];double best=0;PointF[] winner=null;
   for(int seed=0;seed<edge.Length;seed++){
    if(!edge[seed]||seen[seed])continue;int head=0,tail=1;queue[0]=seed;seen[seed]=true;var points=new List<PointF>();
    while(head<tail){int i=queue[head++],x=i%w,y=i/w;points.Add(new PointF(x,y));for(int yy=-1;yy<=1;yy++)for(int xx=-1;xx<=1;xx++){int nx=x+xx,ny=y+yy;if(nx<0||ny<0||nx>=w||ny>=h)continue;int n=ny*w+nx;if(edge[n]&&!seen[n]){seen[n]=true;queue[tail++]=n;}}}
    if(points.Count<Math.Max(30,(w+h)/5))continue;var hull=Hull(points);if(hull.Count<4)continue;var q=Quad(hull);if(!ValidCorners(q,small.Size))continue;
    double area=Area(q)/(w*h);if(area<.025||area>.94)continue;double fit=Area(q)/Math.Max(1,Area(hull.ToArray()));if(fit<.70)continue;
    int support=0;for(int side=0;side<4;side++)for(int t=0;t<40;t++){var a=q[side];var b=q[(side+1)%4];int x=(int)Math.Round(a.X+(b.X-a.X)*t/39),y=(int)Math.Round(a.Y+(b.Y-a.Y)*t/39);bool found=false;for(int dy=-2;dy<=2&&!found;dy++)for(int dx=-2;dx<=2;dx++){int nx=x+dx,ny=y+dy;if(nx>=0&&ny>=0&&nx<w&&ny<h&&edge[ny*w+nx]){found=true;break;}}if(found)support++;}
    double backed=support/160.0;if(backed<.62)continue;double score=.45*backed+.30*fit+.25*Math.Sqrt(area);
    if(score>best){best=score;winner=q;}
   }
   if(winner==null||best<.70)return fallback;
   for(int i=0;i<4;i++)winner[i]=new PointF(winner[i].X*(original.Width-1)/(w-1),winner[i].Y*(original.Height-1)/(h-1));
   return new RectangleProposal(){Corners=winner,Confidence=best,Found=true};
  }
 }
 public static bool NeedsStraightening(RectangleProposal proposal){
  if(!proposal.Found||proposal.Confidence<.78)return false;var q=proposal.Corners;
  double angle=Math.Abs(Math.Atan2(q[1].Y-q[0].Y,q[1].X-q[0].X)*180/Math.PI);
  return angle>2||Math.Abs(Distance(q[0],q[1])-Distance(q[3],q[2]))/Math.Max(1,Distance(q[0],q[1]))>.05;
 }
 static byte[] ReadPixels(Bitmap image){var data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{var bytes=new byte[image.Width*image.Height*4];for(int y=0;y<image.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),bytes,y*image.Width*4,image.Width*4);return bytes;}finally{image.UnlockBits(data);}}
 static void WritePixels(Bitmap image,byte[] bytes){var data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{for(int y=0;y<image.Height;y++)Marshal.Copy(bytes,y*image.Width*4,IntPtr.Add(data.Scan0,y*data.Stride),image.Width*4);}finally{image.UnlockBits(data);}}
 public static Bitmap Perspective(Bitmap original,PointF[] q,float aspect,int maxSide){
  if(!ValidCorners(q,original.Size))throw new ArgumentException("Les quatre coins doivent former un contour convexe, sans croisement.");
  double width=(Distance(q[0],q[1])+Distance(q[3],q[2]))/2+1,height=(Distance(q[0],q[3])+Distance(q[1],q[2]))/2+1;
  if(aspect>0)width=height*aspect;double factor=Math.Min(1,maxSide/Math.Max(width,height));int w=Math.Max(2,(int)Math.Round(width*factor)),h=Math.Max(2,(int)Math.Round(height*factor));
  if((long)w*h>32000000)throw new ArgumentException("La correction dépasse la taille maximale de 32 millions de pixels.");
  double dx1=q[1].X-q[2].X,dx2=q[3].X-q[2].X,dy1=q[1].Y-q[2].Y,dy2=q[3].Y-q[2].Y;
  double sx=q[0].X-q[1].X+q[2].X-q[3].X,sy=q[0].Y-q[1].Y+q[2].Y-q[3].Y,g=0,j=0;
  if(Math.Abs(sx)+Math.Abs(sy)>.00001){double den=dx1*dy2-dx2*dy1;if(Math.Abs(den)<.00001)throw new ArgumentException("Perspective trop déformée.");g=(sx*dy2-dx2*sy)/den;j=(dx1*sy-sx*dy1)/den;}
  double a=q[1].X-q[0].X+g*q[1].X,b=q[3].X-q[0].X+j*q[3].X,c=q[0].X,d=q[1].Y-q[0].Y+g*q[1].Y,e=q[3].Y-q[0].Y+j*q[3].Y,f=q[0].Y;
  using(var source=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb)){
   using(var graphics=Graphics.FromImage(source)){graphics.CompositingMode=CompositingMode.SourceCopy;graphics.DrawImageUnscaled(original,0,0);}var input=ReadPixels(source);var output=new byte[w*h*4];
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){
    double u=x/(double)(w-1),v=y/(double)(h-1),den=g*u+j*v+1,px=(a*u+b*v+c)/den,py=(d*u+e*v+f)/den;
    px=Math.Max(0,Math.Min(source.Width-1,px));py=Math.Max(0,Math.Min(source.Height-1,py));int x0=(int)px,y0=(int)py,x1=Math.Min(source.Width-1,x0+1),y1=Math.Min(source.Height-1,y0+1),at=(y*w+x)*4;double tx=px-x0,ty=py-y0;
    int p0=(y0*source.Width+x0)*4,p1=(y0*source.Width+x1)*4,p2=(y1*source.Width+x0)*4,p3=(y1*source.Width+x1)*4;double w0=(1-tx)*(1-ty),w1=tx*(1-ty),w2=(1-tx)*ty,w3=tx*ty;double alpha=input[p0+3]*w0+input[p1+3]*w1+input[p2+3]*w2+input[p3+3]*w3;output[at+3]=(byte)Math.Round(alpha);
    if(alpha>0)for(int channel=0;channel<3;channel++){double value=input[p0+channel]*input[p0+3]*w0+input[p1+channel]*input[p1+3]*w1+input[p2+channel]*input[p2+3]*w2+input[p3+channel]*input[p3+3]*w3;output[at+channel]=(byte)Math.Max(0,Math.Min(255,Math.Round(value/alpha)));}
   }
   var result=new Bitmap(w,h,PixelFormat.Format32bppArgb);WritePixels(result,output);return result;
  }
 }
 public static float[] AutoLight(Bitmap source){
  using(var small=new Bitmap(160,160)){using(var g=Graphics.FromImage(small)){g.Clear(Color.White);g.DrawImage(source,0,0,160,160);}var pixels=ReadPixels(small);var histogram=new int[256];for(int i=0;i<pixels.Length;i+=4)histogram[(pixels[i+2]*77+pixels[i+1]*150+pixels[i]*29)>>8]++;int lo=0,hi=255,count=0;for(int i=0;i<256;i++){count+=histogram[i];if(count>=25600*.02){lo=i;break;}}count=0;for(int i=255;i>=0;i--){count+=histogram[i];if(count>=25600*.02){hi=i;break;}}if(hi-lo<20)return new float[]{0,0};float gain=Math.Max(1,Math.Min(1.6f,230f/(hi-lo)));float offset=(.5f-(lo+hi)/510f)*gain;return new[]{Math.Max(-30,Math.Min(30,offset*100)),(gain-1)*100};}
 }
 public static Color AutoBackground(Bitmap image){
  long r=0,g=0,b=0;int count=0;for(int i=0;i<32;i++){int x=i*(image.Width-1)/31,y=i*(image.Height-1)/31;foreach(var p in new[]{image.GetPixel(x,0),image.GetPixel(x,image.Height-1),image.GetPixel(0,y),image.GetPixel(image.Width-1,y)}){r+=p.R;g+=p.G;b+=p.B;count++;}}
  return Color.FromArgb((int)(r/count*.18+255*.82),(int)(g/count*.18+255*.82),(int)(b/count*.18+255*.82));
 }
 public static Bitmap Render(Bitmap original,ImageAdjustOptions options,int maxSide){
  using(var straight=Perspective(original,options.Corners,options.Aspect,maxSide)){
   var pixels=ReadPixels(straight);double contrast=Math.Max(0,1+options.Contrast/100),saturation=Math.Max(0,1+options.Saturation/100),brightness=options.Brightness/100;
   for(int i=0;i<pixels.Length;i+=4){double light=(pixels[i+2]*.299+pixels[i+1]*.587+pixels[i]*.114)/255;for(int ch=0;ch<3;ch++){double value=light+(pixels[i+ch]/255.0-light)*saturation;pixels[i+ch]=(byte)Math.Max(0,Math.Min(255,Math.Round(((value-.5)*contrast+.5+brightness)*255)));}}WritePixels(straight,pixels);
   double angle=options.Rotation*Math.PI/180;int rw=Math.Max(1,(int)Math.Ceiling(Math.Abs(straight.Width*Math.Cos(angle))+Math.Abs(straight.Height*Math.Sin(angle))-1e-7)),rh=Math.Max(1,(int)Math.Ceiling(Math.Abs(straight.Width*Math.Sin(angle))+Math.Abs(straight.Height*Math.Cos(angle))-1e-7));
   int pad=options.BackgroundMode==1?0:(int)Math.Round(Math.Min(rw,rh)*Math.Max(0,Math.Min(30,options.Padding))/100.0);int width=rw+2*pad,height=rh+2*pad;
   if((long)width*height>48000000)throw new ArgumentException("Le résultat dépasse 48 millions de pixels. Réduisez la marge ou la taille de l’image.");
   var output=new Bitmap(width,height,PixelFormat.Format32bppArgb);using(var graphics=Graphics.FromImage(output)){
    graphics.Clear(options.BackgroundMode==1?Color.Transparent:options.Background);graphics.SmoothingMode=SmoothingMode.AntiAlias;graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
    graphics.TranslateTransform(width/2f,height/2f);graphics.RotateTransform(options.Rotation);
    if(options.BackgroundMode!=1&&options.Shadow&&pad>0)for(int n=10;n>=1;n--)using(var brush=new SolidBrush(Color.FromArgb(3,0,0,0)))graphics.FillRectangle(brush,-straight.Width/2f-n+4,-straight.Height/2f-n+6,straight.Width+2*n,straight.Height+2*n);
    graphics.DrawImage(straight,-straight.Width/2f,-straight.Height/2f,straight.Width,straight.Height);
   }return output;
  }
 }
}

