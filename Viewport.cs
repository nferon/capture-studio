using System;
using System.Drawing;
using System.Windows.Forms;

public partial class Studio {
 bool fitViewport=true,panning;float displayScale=1;PointF viewCenter;Point panStart;PointF panCenter;Button zoomReadout;Bitmap panPreview;
 float FitScale(){var r=ViewRect;return Math.Max(.01f,Math.Min((canvas.Width-48f)/Math.Max(1,r.Width),(canvas.Height-48f)/Math.Max(1,r.Height)));}
 void ResetViewport(){fitViewport=true;panning=false;canvas.Capture=false;canvas.Cursor=Cursors.Default;UpdateViewport();}
 void UpdateViewport(){if(photo==null){scale=1;ox=oy=0;if(zoomReadout!=null)zoomReadout.Text="—";return;}var r=ViewRect;if(fitViewport){displayScale=FitScale();viewCenter=new PointF(r.Left+r.Width/2f,r.Top+r.Height/2f);}scale=displayScale;
  float halfX=Math.Max(1,(canvas.Width-48f)/2)/scale,halfY=Math.Max(1,(canvas.Height-48f)/2)/scale;
  viewCenter.X=r.Width*scale<=canvas.Width-48?r.Left+r.Width/2f:Math.Max(r.Left+halfX,Math.Min(r.Right-halfX,viewCenter.X));
  viewCenter.Y=r.Height*scale<=canvas.Height-48?r.Top+r.Height/2f:Math.Max(r.Top+halfY,Math.Min(r.Bottom-halfY,viewCenter.Y));
  ox=canvas.Width/2f-viewCenter.X*scale;oy=canvas.Height/2f-viewCenter.Y*scale;
  if(zoomReadout!=null)zoomReadout.Text=Math.Round(scale*100)+" %";
 }
 void ZoomAt(float next,Point anchor){if(photo==null||panning||dragging||cropDragging||textTarget!=null)return;UpdateViewport();PointF imagePoint=ImagePoint(anchor);next=Math.Max(.01f,Math.Min(16,next));fitViewport=false;displayScale=next;viewCenter=new PointF(imagePoint.X-(anchor.X-canvas.Width/2f)/next,imagePoint.Y-(anchor.Y-canvas.Height/2f)/next);UpdateViewport();canvas.Invalidate();}
 void FitImage(){fitViewport=true;UpdateViewport();canvas.Invalidate();}
 void ZoomStep(float factor){UpdateViewport();ZoomAt(scale*factor,new Point(canvas.Width/2,canvas.Height/2));}
 void PanDown(object sender,MouseEventArgs e){if(photo==null||(e.Button!=MouseButtons.Middle&&!(spaceHeld&&e.Button==MouseButtons.Left))||dragging||cropDragging||textTarget!=null)return;UpdateViewport();fitViewport=false;if(panPreview!=null)panPreview.Dispose();panPreview=Render();panning=true;panButton=e.Button;panStart=e.Location;panCenter=viewCenter;canvas.Cursor=Cursors.Hand;canvas.Capture=true;}
 void PanMove(object sender,MouseEventArgs e){if(!panning)return;viewCenter=new PointF(panCenter.X-(e.X-panStart.X)/scale,panCenter.Y-(e.Y-panStart.Y)/scale);UpdateViewport();canvas.Invalidate();}
 void EndPan(){panning=false;canvas.Capture=false;canvas.Cursor=spaceHeld?Cursors.Hand:Cursors.Default;if(panPreview!=null){panPreview.Dispose();panPreview=null;}canvas.Invalidate();}
 void InitializeViewport(TableLayoutPanel root){
  var footer=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty,Padding=Padding.Empty};footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,310));root.Controls.Remove(status);root.Controls.Add(footer,0,2);footer.Controls.Add(status,0,0);
  var controls=new FlowLayoutPanel(){Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty,Padding=Padding.Empty,BackColor=BackColor};footer.Controls.Add(controls,1,0);
  controls.Controls.Add(new Label(){Text="Zoom",AutoSize=true,ForeColor=Color.SlateGray,Margin=new Padding(0,8,5,0)});
  var minus=ModernButton(controls,"−","",delegate{ZoomStep(.8f);},32,false);minus.Height=26;minus.BackColor=BackColor;tips.SetToolTip(minus,"Réduire l’affichage");
  zoomReadout=ModernButton(controls,"—","",delegate{},70,false);zoomReadout.Height=26;zoomReadout.BackColor=BackColor;var menu=new ContextMenuStrip();foreach(int percentage in new int[]{25,50,100,150,200,400,800,1600}){int value=percentage;menu.Items.Add(value+" %",null,delegate{ZoomAt(value/100f,new Point(canvas.Width/2,canvas.Height/2));});}zoomReadout.Click+=delegate{menu.Show(zoomReadout,new Point(0,zoomReadout.Height));};tips.SetToolTip(zoomReadout,"Zoom d’affichage : 100 % = un pixel de l’image par pixel de l’écran");
  var plus=ModernButton(controls,"+","",delegate{ZoomStep(1.25f);},32,false);plus.Height=26;plus.BackColor=BackColor;
  var fit=ModernButton(controls,"Ajuster","",FitImage,78,false);fit.Height=26;fit.BackColor=BackColor;tips.SetToolTip(fit,"Afficher la capture entière");
  canvas.MouseWheel+=delegate(object sender,MouseEventArgs e){if(e.Delta!=0){UpdateViewport();ZoomAt(scale*(float)Math.Pow(1.25,e.Delta/120.0),e.Location);}};
  canvas.MouseDown+=PanDown;canvas.MouseMove+=PanMove;canvas.MouseUp+=delegate(object sender,MouseEventArgs e){if(panning&&e.Button==panButton)EndPan();};canvas.MouseCaptureChanged+=delegate{if(panning&&!canvas.Capture)EndPan();};
  canvas.MouseEnter+=delegate{if(photo!=null&&!capturing&&textTarget==null)canvas.Focus();};canvas.MouseDown+=delegate{canvas.Focus();};canvas.Resize+=delegate{UpdateViewport();};tips.SetToolTip(canvas,"Molette : zoom sous le pointeur • Clic molette maintenu : déplacer la vue • Ajuster : voir toute la capture");UpdateViewport();
 }
}
