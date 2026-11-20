using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

public partial class Studio {
 bool adjustmentsOpen;
 void OpenImageAdjustments(){OpenImageAdjustments(null);}
 void OpenImageAdjustments(RectangleProposal proposal){
  if(photo==null||adjustmentsOpen||CaptureHost.capturing||CaptureHost.ocrBusy)return;
  FinishInlineText(true);adjustmentsOpen=true;
  try{using(var snapshot=Render())using(var dialog=new ImageAdjustmentsDialog(snapshot,Icon,proposal)){
    if(dialog.ShowDialog(this)!=DialogResult.OK)return;
    var result=dialog.TakeResult();if(result==null)return;Studio window=null;
    try{window=CreateCaptureWindow();window.SetPhoto(result);window.dirty=true;window.Show();window.Activate();window.status.Text="Image corrigée — l’original et ses annotations restent dans leur fenêtre.";}
    catch{if(window!=null)window.Dispose();else result.Dispose();throw;}
   }}catch(Exception ex){MessageBox.Show(this,ex.Message,"Redressement de l’image");}finally{adjustmentsOpen=false;}
 }
 async void SuggestImageAdjustment(){
  if(!CaptureHost.preferences.SuggestStraightening||photo==null||adjustmentsOpen)return;
  var original=photo;var originalCrop=crop;
  try{RectangleProposal proposal;using(var snapshot=Render())proposal=await Task.Run(()=>ImageProcessing.DetectRectangle(snapshot));
   if(!IsDisposed&&ReferenceEquals(photo,original)&&crop==originalCrop&&Enabled&&!CaptureHost.capturing&&!CaptureHost.ocrBusy&&ImageProcessing.NeedsStraightening(proposal))OpenImageAdjustments(proposal);
  }catch{if(!IsDisposed)status.Text="Recadrage conservé. Redresser et améliorer reste disponible dans le menu Image.";}
 }
}

sealed class CornerSelectionCanvas:Control {
 readonly Bitmap image;PointF[] corners;int dragging=-1;
 public event EventHandler Changed;
 public PointF[] Corners {get{return (PointF[])corners.Clone();}set{corners=(PointF[])value.Clone();Invalidate();if(Changed!=null)Changed(this,EventArgs.Empty);}}
 public CornerSelectionCanvas(Bitmap source){image=source;corners=ImageProcessing.FullImage(image.Size);DoubleBuffered=true;BackColor=StudioTheme.Canvas;Resize+=delegate{Invalidate();};}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;var display=OcrSelectionCanvas.ImageDisplayBounds(image.Size,ClientSize);float scale=display.Width/image.Width;for(int i=0;i<4;i++){float x=display.X+corners[i].X*scale,y=display.Y+corners[i].Y*scale;if(Math.Abs(e.X-x)<14&&Math.Abs(e.Y-y)<14){dragging=i;Capture=true;break;}}}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging<0)return;var point=OcrSelectionCanvas.ImagePosition(e.Location,image.Size,OcrSelectionCanvas.ImageDisplayBounds(image.Size,ClientSize));point.X=Math.Min(image.Width-1,point.X);point.Y=Math.Min(image.Height-1,point.Y);corners[dragging]=point;Invalidate();if(Changed!=null)Changed(this,EventArgs.Empty);}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=-1;Capture=false;}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var display=OcrSelectionCanvas.ImageDisplayBounds(image.Size,ClientSize);var g=e.Graphics;g.DrawImage(image,display);float scale=display.Width/image.Width;var points=new PointF[4];for(int i=0;i<4;i++)points[i]=new PointF(display.X+corners[i].X*scale,display.Y+corners[i].Y*scale);
  using(var path=new System.Drawing.Drawing2D.GraphicsPath()){path.AddPolygon(points);using(var region=new Region(display)){region.Exclude(path);using(var shade=new SolidBrush(Color.FromArgb(115,0,0,0)))g.FillRegion(shade,region);}}
  using(var pen=new Pen(Color.FromArgb(115,87,255),2))g.DrawPolygon(pen,points);
  for(int i=0;i<4;i++){g.FillEllipse(Brushes.White,points[i].X-9,points[i].Y-9,18,18);using(var brush=new SolidBrush(Color.FromArgb(115,87,255)))g.FillEllipse(brush,points[i].X-6,points[i].Y-6,12,12);}
 }
}

sealed class ImageAdjustmentsDialog:Form {
 readonly Bitmap source;readonly CornerSelectionCanvas corners;readonly PictureBox preview;readonly Label info;
 readonly NumericUpDown rotation=new NumericUpDown(),brightness=new NumericUpDown(),contrast=new NumericUpDown(),saturation=new NumericUpDown(),padding=new NumericUpDown();
 readonly ComboBox background=new ComboBox(),aspect=new ComboBox();readonly CheckBox shadow=new CheckBox();readonly Button apply;readonly Timer refresh=new Timer(){Interval=180};
 Color customColor;Bitmap result;bool changing,rendering;Control editingControls,footerControls;
 public ImageAdjustmentsDialog(Bitmap image,Icon icon,RectangleProposal initial){
  source=image;customColor=ImageProcessing.AutoBackground(image);Text="Redresser et améliorer — Capture Studio";Icon=icon;Font=new Font("Segoe UI",10);ShowInTaskbar=false;StartPosition=FormStartPosition.CenterParent;BackColor=StudioTheme.Background;
  var work=Screen.FromPoint(Cursor.Position).WorkingArea;Size=new Size(Math.Min(1400,work.Width-32),Math.Min(900,work.Height-32));MinimumSize=new Size(Math.Min(1000,work.Width-32),Math.Min(620,work.Height-32));
  var footer=new FlowLayoutPanel(){Dock=DockStyle.Bottom,Height=58,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(12,8,12,4),BackColor=StudioTheme.Surface};Controls.Add(footer);footerControls=footer;
  var cancel=new Button(){Text="Annuler",Width=110,Height=34,DialogResult=DialogResult.Cancel};StyleButton(cancel);footer.Controls.Add(cancel);CancelButton=cancel;
  apply=new Button(){Text="Créer l’image corrigée",Width=210,Height=34};StyleButton(apply);apply.BackColor=Color.FromArgb(115,87,255);apply.ForeColor=Color.White;footer.Controls.Add(apply);apply.Click+=async delegate{await ApplyResult();};footer.Controls.Add(new Label(){Text="La correction crée une copie. Les annotations actuelles seront intégrées à cette copie.",Width=520,Height=36,ForeColor=StudioTheme.Secondary,Font=new Font("Segoe UI",8.5f),Padding=new Padding(4,0,0,0)});
  var content=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(10)};content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,280));Controls.Add(content);editingControls=content;content.BringToFront();
  var split=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=2,RowCount=2};split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));split.RowStyles.Add(new RowStyle(SizeType.Absolute,42));split.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.Controls.Add(split,0,0);
  split.Controls.Add(new Label(){Text="1. Ajustez les quatre coins sur l’objet",Dock=DockStyle.Fill,ForeColor=StudioTheme.Text},0,0);split.Controls.Add(new Label(){Text="2. Vérifiez le résultat",Dock=DockStyle.Fill,ForeColor=StudioTheme.Text},1,0);
  corners=new CornerSelectionCanvas(image){Dock=DockStyle.Fill};split.Controls.Add(corners,0,1);preview=new PictureBox(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=StudioTheme.Canvas};split.Controls.Add(preview,1,1);
  var settings=new FlowLayoutPanel(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(10,0,4,0)};content.Controls.Add(settings,1,0);
  AddHeading(settings,"REDRESSEMENT");AddButton(settings,"Détecter un rectangle",Detect);
  AddButton(settings,"Reprendre toute l’image",delegate{corners.Corners=ImageProcessing.FullImage(source.Size);info.Text="Contour complet. Déplacez les coins si nécessaire.";});
  info=new Label(){Width=238,Height=64,ForeColor=StudioTheme.Secondary,Text="Détection locale des contours…"};settings.Controls.Add(info);
  AddHeading(settings,"PROPORTIONS");aspect.DropDownStyle=ComboBoxStyle.DropDownList;aspect.Width=238;aspect.FlatStyle=FlatStyle.Flat;aspect.BackColor=StudioTheme.Surface;aspect.ForeColor=StudioTheme.Text;aspect.Items.AddRange(new object[]{"Estimées depuis les quatre côtés","Portrait 9:19,5","Portrait 9:16","Paysage 16:9","Photo 4:3"});aspect.SelectedIndex=0;settings.Controls.Add(aspect);aspect.SelectedIndexChanged+=delegate{Schedule();};
  AddHeading(settings,"ROTATION");rotation.Minimum=-180;rotation.Maximum=180;rotation.DecimalPlaces=1;rotation.Increment=.5M;rotation.Width=120;rotation.BackColor=StudioTheme.Surface;rotation.ForeColor=StudioTheme.Text;settings.Controls.Add(rotation);rotation.ValueChanged+=delegate{Schedule();};
  var turns=new FlowLayoutPanel(){Width=240,Height=36};settings.Controls.Add(turns);var left=new Button(){Text="−90°",Width=105};var right=new Button(){Text="+90°",Width=105};StyleButton(left);StyleButton(right);turns.Controls.Add(left);turns.Controls.Add(right);left.Click+=delegate{Turn(-90);};right.Click+=delegate{Turn(90);};
  AddHeading(settings,"LUMIÈRE ET COULEURS");AddButton(settings,"Améliorer automatiquement",Improve);
  AddAdjustment(settings,"Luminosité",brightness);AddAdjustment(settings,"Contraste",contrast);AddAdjustment(settings,"Saturation",saturation);
  AddHeading(settings,"FOND");background.DropDownStyle=ComboBoxStyle.DropDownList;background.Width=238;background.FlatStyle=FlatStyle.Flat;background.BackColor=StudioTheme.Surface;background.ForeColor=StudioTheme.Text;background.Items.AddRange(new object[]{"Couleur automatique","Aucun fond ajouté","Couleur personnalisée"});background.SelectedIndex=0;settings.Controls.Add(background);background.SelectedIndexChanged+=delegate{Schedule();};
  AddButton(settings,"Choisir la couleur…",delegate{using(var picker=new ColorDialog(){Color=customColor})if(picker.ShowDialog(this)==DialogResult.OK){customColor=picker.Color;background.SelectedIndex=2;Schedule();}});
  AddHeading(settings,"MARGE (%)");padding.Minimum=0;padding.Maximum=30;padding.Value=8;settings.Controls.Add(new ValueSlider(padding){Unit="%",ForeColor=StudioTheme.Text});padding.ValueChanged+=delegate{Schedule();};
  shadow.Text="Ombre légère";shadow.Checked=true;shadow.AutoSize=true;shadow.ForeColor=StudioTheme.Text;settings.Controls.Add(shadow);shadow.CheckedChanged+=delegate{Schedule();};
  AddButton(settings,"Réinitialiser les réglages",delegate{changing=true;rotation.Value=brightness.Value=contrast.Value=saturation.Value=0;aspect.SelectedIndex=0;background.SelectedIndex=0;padding.Value=8;shadow.Checked=true;changing=false;Schedule();});
  corners.Changed+=delegate{Schedule();};refresh.Tick+=delegate{refresh.Stop();UpdatePreview();};
  Shown+=delegate{if(initial!=null){corners.Corners=initial.Corners;info.Text=initial.Found?"Contour proposé. Vérifiez et ajustez les coins.":"Aucun contour fiable : ajustez les quatre coins.";UpdatePreview();}else Detect();};
  FormClosing+=delegate(object sender,FormClosingEventArgs e){if(rendering){e.Cancel=true;return;}refresh.Stop();};
 }
 static void StyleButton(Button button){button.ForeColor=StudioTheme.Text;button.BackColor=StudioTheme.Surface;button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderColor=StudioTheme.Track;}
 void AddHeading(FlowLayoutPanel panel,string text){panel.Controls.Add(new Label(){Text=text,Width=238,AutoSize=false,Height=28,ForeColor=StudioTheme.Secondary,Font=new Font("Segoe UI",9,FontStyle.Bold),Padding=new Padding(0,9,0,0)});}
 void AddButton(FlowLayoutPanel panel,string text,Action action){var button=new Button(){Text=text,Width=238,Height=34};StyleButton(button);panel.Controls.Add(button);button.Click+=delegate{try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,Text);}};}
 void AddAdjustment(FlowLayoutPanel panel,string label,NumericUpDown value){panel.Controls.Add(new Label(){Text=label,Width=238,Height=22,ForeColor=StudioTheme.Text});value.Minimum=-100;value.Maximum=100;panel.Controls.Add(new ValueSlider(value){ForeColor=StudioTheme.Text});value.ValueChanged+=delegate{Schedule();};}
 void Turn(int amount){decimal next=rotation.Value+amount;while(next>180)next-=360;while(next< -180)next+=360;rotation.Value=next;}
 void Schedule(){if(changing||rendering)return;refresh.Stop();refresh.Start();}
 ImageAdjustOptions Options(){float[] ratios={0,9f/19.5f,9f/16,16f/9,4f/3};return new ImageAdjustOptions(){Corners=corners.Corners,Rotation=(float)rotation.Value,Brightness=(float)brightness.Value,Contrast=(float)contrast.Value,Saturation=(float)saturation.Value,Aspect=ratios[Math.Max(0,aspect.SelectedIndex)],BackgroundMode=background.SelectedIndex,Background=background.SelectedIndex==0?ImageProcessing.AutoBackground(source):customColor,Padding=(int)padding.Value,Shadow=shadow.Checked};}
 void Detect(){refresh.Stop();var proposal=ImageProcessing.DetectRectangle(source);corners.Corners=proposal.Corners;info.Text=proposal.Found?"Contour proposé. Ajustez les coins avant de valider.":"Aucun contour fiable : ajustez les quatre coins.";refresh.Stop();UpdatePreview();}
 void Improve(){using(var corrected=ImageProcessing.Perspective(source,corners.Corners,0,800)){var values=ImageProcessing.AutoLight(corrected);changing=true;brightness.Value=(decimal)values[0];contrast.Value=(decimal)values[1];changing=false;Schedule();}}
 void UpdatePreview(){if(rendering)return;try{var options=Options();if(!ImageProcessing.ValidCorners(options.Corners,source.Size))throw new ArgumentException("Les coins se croisent. Replacez-les autour de l’objet.");var next=ImageProcessing.Render(source,options,900);var old=preview.Image;preview.Image=next;if(old!=null)old.Dispose();apply.Enabled=true;}catch(Exception ex){info.Text=ex.Message;apply.Enabled=false;}}
 async Task ApplyResult(){
  refresh.Stop();if(rendering)return;rendering=true;apply.Enabled=false;editingControls.Enabled=footerControls.Enabled=false;UseWaitCursor=true;info.Text="Création de l’image corrigée…";
  try{var options=Options();using(var snapshot=new Bitmap(source))result=await Task.Run(()=>ImageProcessing.Render(snapshot,options,8000));rendering=false;DialogResult=DialogResult.OK;Close();}
  catch(Exception ex){rendering=false;apply.Enabled=true;editingControls.Enabled=footerControls.Enabled=true;UseWaitCursor=false;MessageBox.Show(this,ex.Message,"Correction impossible");}
 }
 public Bitmap TakeResult(){var image=result;result=null;return image;}
 protected override void Dispose(bool disposing){if(disposing){refresh.Dispose();if(preview!=null&&preview.Image!=null)preview.Image.Dispose();if(result!=null)result.Dispose();}base.Dispose(disposing);}
}
