using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

public partial class Studio {
 bool ocrBusy;
 void AddOcrMenu(ContextMenuStrip menu){
  var entry=new ToolStripMenuItem("Extraire du texte (OCR)");
  var region=entry.DropDownItems.Add("Sélectionner dans l’image ouverte…",null,delegate{StartOcr(false,true);});
  entry.DropDownItems.Add("Sélectionner sur l’écran…",null,delegate{StartOcr(true,true);});
  var whole=entry.DropDownItems.Add("Toute l’image ouverte",null,delegate{Ocr();});
  entry.DropDownOpening+=delegate{region.Enabled=whole.Enabled=photo!=null;};menu.Items.Add(entry);
 }
 async void StartOcr(bool screen,bool selectRegion){
  if(CaptureHost.ocrBusy||CaptureHost.capturing){status.Text="Une capture ou une extraction de texte est déjà en cours.";return;}
  if(!screen&&photo==null)return;
  CaptureHost.ocrBusy=true;Bitmap input=null;
  try{
   if(screen){
    var hidden=HideCaptureWindows();
    try{
     await Task.Delay(250);var desktop=SystemInformation.VirtualScreen;
     using(var snapshot=new Bitmap(desktop.Width,desktop.Height)){
      using(var graphics=Graphics.FromImage(snapshot))graphics.CopyFromScreen(desktop.Location,Point.Empty,desktop.Size);
      using(var picker=new RegionPicker(snapshot,desktop,"OCR • Glissez sur le texte • Entrée : tout l’écran • Échap : annuler")){
       if(picker.ShowDialog()!=DialogResult.OK)return;
       input=snapshot.Clone(picker.Selection,PixelFormat.Format32bppArgb);
      }
     }
    }finally{RestoreCaptureWindows(hidden);}
   }else{
    FinishInlineText(true);
    using(var snapshot=Render()){
     if(selectRegion){
      using(var picker=new OcrImagePicker(snapshot,Icon)){
       if(picker.ShowDialog(this)!=DialogResult.OK)return;
       input=snapshot.Clone(picker.Selection,PixelFormat.Format32bppArgb);
      }
     }else input=new Bitmap(snapshot);
    }
   }
   status.Text="Extraction du texte de la zone en cours…";
   string text=await ExtractOcrText(input);
   if(IsDisposed)return;
   using(var result=CreateOcrDialog(String.IsNullOrWhiteSpace(text)?"Aucun texte détecté dans cette zone.":text))result.ShowDialog(this);
   status.Text="Texte extrait. Sélectionnez le texte puis Ctrl+C pour le copier.";
  }catch(Exception ex){if(!IsDisposed)MessageBox.Show(this,ex.Message,"Extraction du texte");}
  finally{if(input!=null)input.Dispose();CaptureHost.ocrBusy=false;}
 }
 async Task<string> ExtractOcrText(Bitmap bitmap){
  string path=Path.Combine(Path.GetTempPath(),"CaptureStudio-OCR-"+Guid.NewGuid().ToString("N")+".png"),output=path+".txt";
  try{
   bitmap.Save(path,ImageFormat.Png);
   var info=new System.Diagnostics.ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -File "+QuoteArgument(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ocr.ps1"))+" -InputImage "+QuoteArgument(path)+" -OutputText "+QuoteArgument(output)){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true};
   using(var process=System.Diagnostics.Process.Start(info)){
    string error=await process.StandardError.ReadToEndAsync();await Task.Run(()=>process.WaitForExit());
    if(process.ExitCode!=0)throw new InvalidOperationException("OCR indisponible : "+error);
   }
   return File.ReadAllText(output);
  }finally{try{if(File.Exists(path))File.Delete(path);}catch{}try{if(File.Exists(output))File.Delete(output);}catch{}}
 }
}

sealed class OcrImagePicker:Form {
 readonly OcrSelectionCanvas viewer;
 public Rectangle Selection {get{return viewer.Selection;}}
 public OcrImagePicker(Bitmap image,Icon icon){
  Text="OCR — sélectionner une zone de l’image";Icon=icon;Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;
  var work=Screen.FromPoint(Cursor.Position).WorkingArea;Size=new Size(Math.Min(1000,work.Width-32),Math.Min(740,work.Height-32));MinimumSize=new Size(480,320);BackColor=StudioTheme.Background;
  viewer=new OcrSelectionCanvas(image){Dock=DockStyle.Fill};Controls.Add(viewer);
  var footer=new FlowLayoutPanel(){Dock=DockStyle.Bottom,Height=92,Padding=new Padding(12,8,8,4),BackColor=StudioTheme.Surface};Controls.Add(footer);
  footer.Controls.Add(new Label(){Text="Glissez sur le texte à extraire. L’image d’origine reste intacte.",Width=850,Height=26,ForeColor=StudioTheme.Secondary});
  var accept=new Button(){Text="Extraire la sélection",Width=190,Height=34,Enabled=false};footer.Controls.Add(accept);accept.Click+=delegate{if(!viewer.Selection.IsEmpty){DialogResult=DialogResult.OK;Close();}};AcceptButton=accept;
  var all=new Button(){Text="Toute l’image",Width=145,Height=34};footer.Controls.Add(all);all.Click+=delegate{viewer.SelectAll();DialogResult=DialogResult.OK;Close();};
  var cancel=new Button(){Text="Annuler",Width=105,Height=34,DialogResult=DialogResult.Cancel};footer.Controls.Add(cancel);CancelButton=cancel;
  viewer.SelectionChanged+=delegate{accept.Enabled=!viewer.Selection.IsEmpty;};
  footer.Resize+=delegate{footer.Controls[0].Width=Math.Max(200,footer.ClientSize.Width-footer.Padding.Horizontal-12);};
 }
}

sealed class OcrSelectionCanvas:Control {
 readonly Bitmap image;
 PointF start,current;bool selecting;
 public Rectangle Selection {get;private set;}
 public event EventHandler SelectionChanged;
 public OcrSelectionCanvas(Bitmap source){image=source;DoubleBuffered=true;Cursor=Cursors.Cross;BackColor=StudioTheme.Canvas;Resize+=delegate{Invalidate();};}
 public static RectangleF ImageDisplayBounds(Size image,Size client){
  float scale=Math.Min(Math.Max(1,client.Width-24)/(float)image.Width,Math.Max(1,client.Height-24)/(float)image.Height);
  return new RectangleF((client.Width-image.Width*scale)/2,(client.Height-image.Height*scale)/2,image.Width*scale,image.Height*scale);
 }
 public static PointF ImagePosition(Point client,Size source,RectangleF display){return new PointF(Math.Max(0,Math.Min(source.Width,(client.X-display.X)*source.Width/display.Width)),Math.Max(0,Math.Min(source.Height,(client.Y-display.Y)*source.Height/display.Height)));}
 public static Rectangle SelectionBounds(PointF from,PointF to,Size source){
  var bounds=Rectangle.Intersect(Rectangle.FromLTRB((int)Math.Floor(Math.Min(from.X,to.X)),(int)Math.Floor(Math.Min(from.Y,to.Y)),(int)Math.Ceiling(Math.Max(from.X,to.X)),(int)Math.Ceiling(Math.Max(from.Y,to.Y))),new Rectangle(Point.Empty,source));
  return bounds.Width>=3&&bounds.Height>=3?bounds:Rectangle.Empty;
 }
 public void SelectAll(){Selection=new Rectangle(Point.Empty,image.Size);if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);Invalidate();}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);var display=ImageDisplayBounds(image.Size,ClientSize);if(e.Button!=MouseButtons.Left||!display.Contains(e.Location))return;start=current=ImagePosition(e.Location,image.Size,display);selecting=true;Capture=true;Selection=Rectangle.Empty;Invalidate();}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!selecting)return;current=ImagePosition(e.Location,image.Size,ImageDisplayBounds(image.Size,ClientSize));Selection=SelectionBounds(start,current,image.Size);Invalidate();}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(!selecting||e.Button!=MouseButtons.Left)return;current=ImagePosition(e.Location,image.Size,ImageDisplayBounds(image.Size,ClientSize));Selection=SelectionBounds(start,current,image.Size);selecting=false;Capture=false;if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);Invalidate();}
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);var display=ImageDisplayBounds(image.Size,ClientSize);var g=e.Graphics;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(image,display);
  if(!Selection.IsEmpty){
   float scale=display.Width/image.Width;var selected=new RectangleF(display.X+Selection.X*scale,display.Y+Selection.Y*scale,Selection.Width*scale,Selection.Height*scale);
   using(var outside=new Region(display)){outside.Exclude(selected);using(var dim=new SolidBrush(Color.FromArgb(120,0,0,0)))g.FillRegion(dim,outside);}
   using(var border=new Pen(Color.FromArgb(115,87,255),2))g.DrawRectangle(border,selected.X,selected.Y,selected.Width,selected.Height);
  }
 }
}
