using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class EditState {
 public List<Mark> Marks; public int CropX,CropY,CropWidth,CropHeight,Blur;
}
public partial class Studio {
 Rectangle crop; Bitmap blurred; int blurAmount; bool cropping,cropDragging; PointF cropStart; RectangleF cropDraft;
 HashSet<Mark> selection=new HashSet<Mark>(); bool capturing;
 CheckBox linkBox=new CheckBox(){Text="Afficher le trait de liaison",AutoSize=true}; Button lensHere,elementActions;
 NumericUpDown blurControl=new NumericUpDown(); Label blurLabel=new Label();
 NotifyIcon tray; bool hotkeyActive; ToolStripMenuItem hotkeyMenu;
 const int CaptureHotkey=7412;
 [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
 [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr window,int id);
 Rectangle ViewRect {get {return crop.Width>0&&crop.Height>0?crop:photo==null?Rectangle.Empty:new Rectangle(Point.Empty,photo.Size);}}
 void CreateElementActions(Control inspector){
  var menu=new ContextMenuStrip();menu.Items.Add("Modifier le texte…",null,delegate{EditText();});menu.Items.Add("Dupliquer",null,delegate{if(selected==null)return;Checkpoint();var m=selected.Copy();TranslateMark(m,25,25);marks.Add(m);selection.Clear();selection.Add(m);selected=m;Renumber();RefreshSelection();canvas.Invalidate();});
  menu.Items.Add("Mettre au premier plan",null,delegate{if(selected!=null){Checkpoint();marks.Remove(selected);marks.Add(selected);canvas.Invalidate();}});menu.Items.Add("Mettre à l’arrière-plan",null,delegate{if(selected!=null){Checkpoint();marks.Remove(selected);marks.Insert(0,selected);canvas.Invalidate();}});menu.Items.Add("Supprimer la sélection",null,delegate{Delete();});
  menu.Opening+=delegate{menu.Items[0].Enabled=selected!=null&&selected.Kind=="Texte";};elementActions=Add(inspector,"Actions de l’élément ▾",delegate{});elementActions.Click+=delegate{if(selected!=null)menu.Show(elementActions,new Point(0,elementActions.Height));};canvas.ContextMenuStrip=menu;
 }
 void InitializeFeatures(Control tools,FlowLayoutPanel inspector,Control files){
  linkBox.Margin=new Padding(0,8,0,8);inspector.Controls.Add(linkBox);inspector.Controls.SetChildIndex(linkBox,inspector.Controls.GetChildIndex(zoomRow)+1);
  linkBox.CheckedChanged+=delegate{if(!syncing&&selected!=null&&selected.Kind=="Loupe"){Checkpoint();selected.Link=linkBox.Checked;canvas.Invalidate();}};
  lensHere=Add(inspector,"Poser la loupe sur sa cible",delegate{if(selected!=null&&selected.Kind=="Loupe"){Checkpoint();selected.X=selected.SX;selected.Y=selected.SY;selected.Link=false;selected.InPlace=true;RefreshSelection();canvas.Invalidate();}});tips.SetToolTip(lensHere,"Superpose le cercle agrandi au détail d’origine, sans liaison.");inspector.Controls.SetChildIndex(lensHere,inspector.Controls.GetChildIndex(linkBox)+1);
  blurLabel.Text="Flou de l’arrière-plan (0 = aucun)";blurLabel.AutoSize=true;blurLabel.Margin=new Padding(0,12,0,5);inspector.Controls.Add(blurLabel);blurControl.Minimum=0;blurControl.Maximum=24;blurControl.Width=195;inspector.Controls.Add(blurControl);
  inspector.Controls.SetChildIndex(blurLabel,1);inspector.Controls.SetChildIndex(blurControl,2);
  blurControl.ValueChanged+=delegate{if(!syncing&&photo!=null){Checkpoint();blurAmount=(int)blurControl.Value;ClearBlur();canvas.Invalidate();}};
  tips.SetToolTip(blurControl,"Le fond est flouté ; les loupes et les zones nettes gardent leurs détails.");
  var menu=new ContextMenuStrip();
  menu.Items.Add("Importer une image…",null,delegate{Import();});menu.Items.Add("Ouvrir une image ou un projet… (Ctrl+O)",null,delegate{OpenProject();});menu.Items.Add("Enregistrer le projet… (Ctrl+S)",null,delegate{SaveProject();});menu.Items.Add("Exporter en PNG… (Ctrl+Maj+S / Alt+Maj+S)",null,delegate{Export();});AddOcrMenu(menu);menu.Items.Add("Vider la capture",null,delegate{ClearCapture();});menu.Items.Add(new ToolStripSeparator());
  menu.Items.Add("Rétablir le cadrage original",null,delegate{if(photo!=null){Checkpoint();crop=Rectangle.Empty;ResetViewport();canvas.Invalidate();}});
  menu.Items.Add("Sélectionner toute l’image (Ctrl+A)",null,delegate{SelectImage();});menu.Items.Add("Sélectionner toutes les annotations",null,delegate{SelectAllMarks();});
  var align=new ToolStripMenuItem("Aligner / répartir la sélection");
  foreach(string name in new string[]{"À gauche","Au centre horizontal","À droite","En haut","Au centre vertical","En bas","Espacer horizontalement","Espacer verticalement"}){string command=name;align.DropDownItems.Add(name,null,delegate{AlignSelection(command);});}menu.Items.Add(align);
  menu.Items.Add(new ToolStripSeparator());hotkeyMenu=new ToolStripMenuItem("Utiliser Impr. écran",null,delegate{ToggleHotkey();});
  InitializeWindowsMenu(menu);
  menu.Items.Add("Nouvelle fenêtre (Ctrl+N)",null,delegate{OpenNewWindow(new string[0]);});
  menu.Items.Add("Masquer près de l’horloge",null,delegate{CaptureHost.EnsureTray();Hide();});
  var more=Add(files,"Fichier",delegate{});more.Width=76;more.Click+=delegate{menu.Show(more,new Point(0,more.Height));};tips.SetToolTip(more,"Fichiers, alignement, cadrage et raccourci Impr. écran");
  canvas.MouseUp+=delegate{if(cropDragging){cropDragging=false;status.Text="Ajustez les coins du cadre • Entrée pour recadrer • Échap pour annuler";}snapX=snapY=null;canvas.Invalidate();};
  FormClosed+=delegate{if(hotkeyActive)UnregisterHotKey(Handle,activeHotkeyId);if(tray!=null)tray.Dispose();ClearBlur();if(photo!=null)photo.Dispose();};
 }
 void RefreshFeatures(){bool lens=selected!=null&&selected.Kind=="Loupe";linkBox.Visible=lens;if(lensHere!=null)lensHere.Visible=lens;if(elementActions!=null)elementActions.Enabled=selected!=null;if(selected!=null)linkBox.Checked=selected.Link;blurControl.Value=blurAmount;blurControl.Enabled=photo!=null;RefreshModernProperties();RefreshLocalEffects();}
 void ClearBlur(){ClearLocalBlur();if(blurred!=null){blurred.Dispose();blurred=null;}}
 void ResetFeatures(){imageSelected=false;crop=Rectangle.Empty;blurAmount=0;cropping=false;selection.Clear();ClearBlur();ResetViewport();}
 string FeatureState(){return json.Serialize(new EditState(){Marks=marks,CropX=crop.X,CropY=crop.Y,CropWidth=crop.Width,CropHeight=crop.Height,Blur=blurAmount});}
 void RestoreState(string data){var state=json.Deserialize<EditState>(data);marks=state.Marks;crop=new Rectangle(state.CropX,state.CropY,state.CropWidth,state.CropHeight);blurAmount=state.Blur;selected=null;selection.Clear();cropping=false;ClearBlur();ResetViewport();}
 void DrawBackground(Graphics g){
  if(blurAmount==0){g.DrawImageUnscaled(photo,0,0);return;}
  if(blurred==null)blurred=MakeBlur(photo,blurAmount);
  g.DrawImageUnscaled(blurred,0,0);using(var path=new GraphicsPath(FillMode.Winding)){
   foreach(var mark in marks){if(mark.Kind=="Loupe"){float r=mark.Size/(2*Math.Max(1,mark.Zoom));path.AddEllipse(mark.SX-r,mark.SY-r,r*2,r*2);}else if(mark.Kind=="Zone nette")path.AddRectangle(MarkBounds(mark));}
   if(path.PointCount>0){var saved=g.Save();g.SetClip(path,CombineMode.Intersect);g.DrawImageUnscaled(photo,0,0);g.Restore(saved);}
  }
 }
 public static Bitmap MakeBlur(Bitmap source,int radius){
  int w=source.Width,h=source.Height;var result=new Bitmap(w,h,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(result))g.DrawImageUnscaled(source,0,0);
  var data=result.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);int stride=data.Stride;var input=new byte[stride*h];Marshal.Copy(data.Scan0,input,0,input.Length);var temp=new byte[input.Length];
  int r=Math.Max(1,radius),diameter=2*r+1;
  for(int pass=0;pass<3;pass++){
   for(int y=0;y<h;y++)for(int channel=0;channel<4;channel++){int total=0;for(int k=-r;k<=r;k++)total+=input[y*stride+Math.Max(0,Math.Min(w-1,k))*4+channel];for(int x=0;x<w;x++){temp[y*stride+x*4+channel]=(byte)(total/diameter);total-=input[y*stride+Math.Max(0,x-r)*4+channel];total+=input[y*stride+Math.Min(w-1,x+r+1)*4+channel];}}
   for(int x=0;x<w;x++)for(int channel=0;channel<4;channel++){int total=0;for(int k=-r;k<=r;k++)total+=temp[Math.Max(0,Math.Min(h-1,k))*stride+x*4+channel];for(int y=0;y<h;y++){input[y*stride+x*4+channel]=(byte)(total/diameter);total-=temp[Math.Max(0,y-r)*stride+x*4+channel];total+=temp[Math.Min(h-1,y+r+1)*stride+x*4+channel];}}
  }
  Marshal.Copy(input,0,data.Scan0,input.Length);result.UnlockBits(data);return result;
 }
 RectangleF MarkBounds(Mark m){if(m.Kind=="Loupe")return new RectangleF(m.X-m.Size/2,m.Y-m.Size/2,m.Size,m.Size);if(m.Kind=="Numéro"){float d=Math.Max(16,m.Size/5)*1.8f;return new RectangleF(m.X-d/2,m.Y-d/2,d,d);}if(m.Kind=="Texte")return new RectangleF(m.X,m.Y,Math.Max(40,(m.Text??"").Length*m.Size/6),m.Size/2);return RectangleF.FromLTRB(Math.Min(m.X,m.BX),Math.Min(m.Y,m.BY),Math.Max(m.X,m.BX),Math.Max(m.Y,m.BY));}
 bool HitMark(Mark m,PointF p){if(m.Kind=="Flèche"){float dx=m.BX-m.X,dy=m.BY-m.Y,len=dx*dx+dy*dy;float t=len==0?0:Math.Max(0,Math.Min(1,((p.X-m.X)*dx+(p.Y-m.Y)*dy)/len));return Dist(p,m.X+t*dx,m.Y+t*dy)<12/scale;}return MarkBounds(m).Contains(p);}
 void TranslateMark(Mark m,float dx,float dy){m.X+=dx;m.Y+=dy;m.BX+=dx;m.BY+=dy;if(m.Kind=="Loupe"&&m.InPlace){m.SX=m.X;m.SY=m.Y;m.Link=false;}}
 void SelectAllMarks(){selection=new HashSet<Mark>(marks);selected=marks.Count==0?null:marks[marks.Count-1];RefreshSelection();canvas.Invalidate();}
 void AlignSelection(string command){
  var group=marks.Where(m=>selection.Contains(m)||m==selected).ToList();if(group.Count==0)return;
  if(command.StartsWith("Espacer")&&group.Count<3){status.Text="Sélectionnez au moins trois éléments avec Ctrl+clic pour répartir les espacements.";return;}
  Checkpoint();RectangleF area=group.Count==1?ViewRect:group.Select(MarkBounds).Aggregate(RectangleF.Union);
  if(command.StartsWith("Espacer")){bool horizontal=command=="Espacer horizontalement";group=group.OrderBy(m=>horizontal?MarkBounds(m).Left:MarkBounds(m).Top).ToList();float total=group.Sum(m=>horizontal?MarkBounds(m).Width:MarkBounds(m).Height);float gap=((horizontal?area.Width:area.Height)-total)/(group.Count-1),next=horizontal?area.Left:area.Top;foreach(var m in group){var b=MarkBounds(m);TranslateMark(m,horizontal?next-b.Left:0,horizontal?0:next-b.Top);next+=(horizontal?b.Width:b.Height)+gap;}}
  else foreach(var m in group){var b=MarkBounds(m);float dx=0,dy=0;if(command=="À gauche")dx=area.Left-b.Left;if(command=="À droite")dx=area.Right-b.Right;if(command=="Au centre horizontal")dx=area.Left+area.Width/2-b.Left-b.Width/2;if(command=="En haut")dy=area.Top-b.Top;if(command=="En bas")dy=area.Bottom-b.Bottom;if(command=="Au centre vertical")dy=area.Top+area.Height/2-b.Top-b.Height/2;TranslateMark(m,dx,dy);}
  canvas.Invalidate();
 }
 float? snapX,snapY;
 void Snap(Mark m){snapX=snapY=null;if((ModifierKeys&Keys.Alt)!=0)return;float tolerance=6/scale;var b=MarkBounds(m);var view=ViewRect;float cx=b.Left+b.Width/2,cy=b.Top+b.Height/2;var xs=new List<float>(){view.Left+view.Width/2};var ys=new List<float>(){view.Top+view.Height/2};foreach(var other in marks){if(other==m||selection.Contains(other))continue;var r=MarkBounds(other);xs.Add(r.Left+r.Width/2);ys.Add(r.Top+r.Height/2);}float bestX=tolerance,bestY=tolerance;foreach(float x in xs)if(Math.Abs(x-cx)<bestX){bestX=Math.Abs(x-cx);snapX=x;}foreach(float y in ys)if(Math.Abs(y-cy)<bestY){bestY=Math.Abs(y-cy);snapY=y;}TranslateMark(m,snapX.HasValue?snapX.Value-cx:0,snapY.HasValue?snapY.Value-cy:0);}
 void BeginCrop(){if(photo==null)return;cropping=true;cropDraft=ViewRect;tool.SelectedIndex=0;status.Text="Glissez pour tracer le cadrage • ajustez les coins • Entrée : valider • Échap : annuler";canvas.Invalidate();}
 void CommitCrop(){if(!cropping||photo==null)return;var r=Rectangle.Intersect(Rectangle.Round(cropDraft),ViewRect);if(r.Width<8||r.Height<8){status.Text="Le cadrage doit mesurer au moins 8 × 8 pixels.";return;}Checkpoint();crop=r;ResetViewport();cropping=false;cropDragging=false;canvas.Invalidate();status.Text="Image recadrée à "+r.Width+" × "+r.Height+" pixels. L’original est conservé dans le projet.";}
 bool CropDown(PointF point){if(!cropping)return false;var corners=new PointF[]{new PointF(cropDraft.Left,cropDraft.Top),new PointF(cropDraft.Right,cropDraft.Top),new PointF(cropDraft.Left,cropDraft.Bottom),new PointF(cropDraft.Right,cropDraft.Bottom)};int nearest=-1;for(int i=0;i<4;i++)if(Dist(point,corners[i].X,corners[i].Y)<14/scale){nearest=i;break;}cropStart=nearest<0?point:corners[3-nearest];cropDragging=true;return true;}
 void CropMove(PointF point){var v=ViewRect;point.X=Math.Max(v.Left,Math.Min(v.Right,point.X));point.Y=Math.Max(v.Top,Math.Min(v.Bottom,point.Y));cropDraft=RectangleF.FromLTRB(Math.Min(cropStart.X,point.X),Math.Min(cropStart.Y,point.Y),Math.Max(cropStart.X,point.X),Math.Max(cropStart.Y,point.Y));canvas.Invalidate();}
 void DrawEditing(Graphics g){using(var line=new Pen(Color.FromArgb(170,115,87,255),1/scale)){line.DashStyle=DashStyle.Dash;if(snapX.HasValue)g.DrawLine(line,snapX.Value,ViewRect.Top,snapX.Value,ViewRect.Bottom);if(snapY.HasValue)g.DrawLine(line,ViewRect.Left,snapY.Value,ViewRect.Right,snapY.Value);foreach(var m in selection)if(m!=selected)g.DrawRectangle(line,Rectangle.Round(MarkBounds(m)));}
  if(cropping){using(var outside=new Region(ViewRect)){outside.Exclude(cropDraft);using(var shade=new SolidBrush(Color.FromArgb(125,0,0,0)))g.FillRegion(shade,outside);}using(var line=new Pen(Color.White,2/scale))g.DrawRectangle(line,Rectangle.Round(cropDraft));DrawHandle(g,cropDraft.Left,cropDraft.Top);DrawHandle(g,cropDraft.Right,cropDraft.Top);DrawHandle(g,cropDraft.Left,cropDraft.Bottom);DrawHandle(g,cropDraft.Right,cropDraft.Bottom);}
 }
 void EnsureTray(){if(tray!=null)return;var menu=new ContextMenuStrip();menu.Items.Add("Ouvrir Capture Studio",null,delegate{Show();WindowState=FormWindowState.Normal;Activate();});menu.Items.Add("Capturer",null,delegate{TakeCapture();});menu.Items.Add("Quitter",null,delegate{QuitApplication();});tray=new NotifyIcon(){Text="Capture Studio",Icon=Icon,Visible=true,ContextMenuStrip=menu};tray.DoubleClick+=delegate{Show();WindowState=FormWindowState.Normal;Activate();};}
 void ToggleHotkey(){string error;if(!ApplyCaptureShortcut(!hotkeyActive,preferences.HotkeyKey,preferences.HotkeyModifiers,out error,true))MessageBox.Show(this,error,"Raccourci indisponible");}
 protected override void WndProc(ref Message message){if(message.Msg==0x312&&message.WParam.ToInt32()==activeHotkeyId&&hotkeyActive){if(!capturing&&Enabled)BeginInvoke(new Action(TakeCapture));return;}base.WndProc(ref message);}
}
