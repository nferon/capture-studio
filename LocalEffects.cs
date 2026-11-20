using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public partial class Studio {
 CheckBox inPlaceBox=new CheckBox(){Text="Loupe sur place (façon iPhone)",AutoSize=true};
 NumericUpDown localSigma=new NumericUpDown(){Minimum=1,Maximum=12,DecimalPlaces=1,Increment=.5M,Value=3};
 FlowLayoutPanel localBlurRow;
 CheckBox blurBorderBox=new CheckBox(){Text="Afficher un contour coloré",AutoSize=true};
 class BlurEntry {public Rectangle Bounds;public float Sigma;public Bitmap Image;}
 Dictionary<Mark,BlurEntry> localBlurCache=new Dictionary<Mark,BlurEntry>();
 void InitializeLocalEffects(FlowLayoutPanel inspector){
  inspector.Controls.Add(inPlaceBox);inspector.Controls.SetChildIndex(inPlaceBox,inspector.Controls.GetChildIndex(zoomRow)+1);
  inPlaceBox.CheckedChanged+=delegate{if(syncing||selected==null||selected.Kind!="Loupe")return;Checkpoint();selected.InPlace=inPlaceBox.Checked;if(selected.InPlace){selected.X=selected.SX;selected.Y=selected.SY;selected.Link=false;}RefreshSelection();canvas.Invalidate();};
  localBlurRow=new FlowLayoutPanel(){Width=226,Height=128,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=Padding.Empty};Heading(localBlurRow,"FLOU GAUSSIEN LOCAL");localBlurRow.Controls.Add(new ValueSlider(localSigma){Unit="σ"});localBlurRow.Controls.Add(blurBorderBox);blurBorderBox.CheckedChanged+=delegate{if(syncing||selected==null||selected.Kind!="Flou local")return;Checkpoint();selected.BlurBorder=blurBorderBox.Checked;RefreshSelection();canvas.Invalidate();};inspector.Controls.Add(localBlurRow);inspector.Controls.SetChildIndex(localBlurRow,1);
  localSigma.ValueChanged+=delegate{if(!syncing&&selected!=null&&selected.Kind=="Flou local"){Checkpoint();selected.BlurSigma=(float)localSigma.Value;canvas.Invalidate();}};
 }
 void RefreshLocalEffects(){if(localBlurRow==null)return;bool lens=selected!=null&&selected.Kind=="Loupe";inPlaceBox.Visible=lens;if(lens)inPlaceBox.Checked=selected.InPlace;linkBox.Enabled=!lens||!selected.InPlace;localBlurRow.Visible=selected!=null&&selected.Kind=="Flou local";if(selected!=null&&selected.Kind=="Flou local"){localSigma.Value=(decimal)Math.Max(1,Math.Min(12,selected.BlurSigma));blurBorderBox.Checked=selected.BlurBorder;}}
 void DrawBlurHandle(Graphics g,float x,float y){float r=5/scale;using(var brush=new SolidBrush(Color.FromArgb(115,87,255)))g.FillRectangle(brush,x-r,y-r,r*2,r*2);}
 void ClearLocalBlur(){foreach(var entry in localBlurCache.Values)entry.Image.Dispose();localBlurCache.Clear();}
 void DrawLocalBlur(Graphics g,Mark mark,bool editing){
  Rectangle bounds=Rectangle.Intersect(Rectangle.Ceiling(MarkBounds(mark)),new Rectangle(Point.Empty,photo.Size));if(bounds.Width<1||bounds.Height<1)return;float sigma=Math.Max(1,Math.Min(12,mark.BlurSigma));BlurEntry entry;
  if(!localBlurCache.TryGetValue(mark,out entry)||entry.Bounds!=bounds||entry.Sigma!=sigma){if(entry!=null)entry.Image.Dispose();int halo=(int)Math.Ceiling(3*sigma);var extended=Rectangle.Intersect(Rectangle.Inflate(bounds,halo,halo),new Rectangle(Point.Empty,photo.Size));using(var part=photo.Clone(extended,PixelFormat.Format32bppArgb))using(var filtered=GaussianBlur(part,sigma)){var local=new Rectangle(bounds.X-extended.X,bounds.Y-extended.Y,bounds.Width,bounds.Height);entry=new BlurEntry(){Bounds=bounds,Sigma=sigma,Image=filtered.Clone(local,PixelFormat.Format32bppArgb)};}localBlurCache[mark]=entry;}
  g.DrawImageUnscaled(entry.Image,bounds.Location);if(mark.BlurBorder){using(var border=new Pen(Color.FromArgb(mark.Color),Math.Max(1,mark.Width))){border.Alignment=System.Drawing.Drawing2D.PenAlignment.Inset;g.DrawRectangle(border,bounds.X,bounds.Y,bounds.Width-1,bounds.Height-1);}}if(editing&&mark==selected){using(var p=new Pen(Color.FromArgb(115,87,255),1.5f/scale)){p.DashStyle=System.Drawing.Drawing2D.DashStyle.Dash;g.DrawRectangle(p,bounds);}}
 }
 public static Bitmap GaussianBlur(Bitmap source,float sigma){
  sigma=Math.Max(.1f,Math.Min(12,sigma));int radius=(int)Math.Ceiling(sigma*3);double[] kernel=new double[radius*2+1];double sum=0;for(int k=-radius;k<=radius;k++){double value=Math.Exp(-(k*k)/(2*sigma*sigma));kernel[k+radius]=value;sum+=value;}for(int k=0;k<kernel.Length;k++)kernel[k]/=sum;
  var result=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(result))g.DrawImageUnscaled(source,0,0);var rect=new Rectangle(Point.Empty,result.Size);var data=result.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);try{int w=result.Width,h=result.Height,stride=data.Stride;var pixels=new byte[stride*h];var temp=new float[pixels.Length];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
   for(int y=0;y<h;y++)for(int x=0;x<w;x++)for(int c=0;c<4;c++){double total=0;for(int k=-radius;k<=radius;k++)total+=pixels[y*stride+Math.Max(0,Math.Min(w-1,x+k))*4+c]*kernel[k+radius];temp[y*stride+x*4+c]=(float)total;}
   for(int y=0;y<h;y++)for(int x=0;x<w;x++)for(int c=0;c<4;c++){double total=0;for(int k=-radius;k<=radius;k++)total+=temp[Math.Max(0,Math.Min(h-1,y+k))*stride+x*4+c]*kernel[k+radius];pixels[y*stride+x*4+c]=(byte)Math.Max(0,Math.Min(255,Math.Round(total)));}Marshal.Copy(pixels,0,data.Scan0,pixels.Length);
  }finally{result.UnlockBits(data);}return result;
 }
}
