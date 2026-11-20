using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

sealed class ShareAppTile:Button {
 readonly string app;
 readonly Image logo;
 bool hovered;
 public ShareAppTile(string name){
  app=name;Text=name;AccessibleName=name+" — copier l’image et ouvrir l’application";
  Size=new Size(142,142);Margin=new Padding(4);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;
  SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Brand."+name)){
   if(stream!=null)using(var source=Image.FromStream(stream))logo=new Bitmap(source);
  }
 }
 protected override void OnMouseEnter(EventArgs e){hovered=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hovered=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
 protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
  g.Clear(StudioTheme.Surface);
  var bounds=new RectangleF(1,1,Width-3,Height-3);
  using(var path=new GraphicsPath()){
   float r=16;path.AddArc(bounds.Left,bounds.Top,r,r,180,90);path.AddArc(bounds.Right-r,bounds.Top,r,r,270,90);path.AddArc(bounds.Right-r,bounds.Bottom-r,r,r,0,90);path.AddArc(bounds.Left,bounds.Bottom-r,r,r,90,90);path.CloseFigure();
   using(var brush=new SolidBrush(hovered||Focused?StudioTheme.Hover:StudioTheme.Background))g.FillPath(brush,path);
   if(Focused)using(var pen=new Pen(StudioTheme.AccentText,2))g.DrawPath(pen,path);
  }
  int cx=(Width-64)/2;
  using(var brush=new SolidBrush(app=="Discord"?Color.FromArgb(88,101,242):Color.FromArgb(26,177,81)))g.FillEllipse(brush,cx,12,64,64);
  if(logo!=null){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(logo,new Rectangle(cx+12,24,40,40));}
  using(var font=new Font("Segoe UI",10,FontStyle.Bold))TextRenderer.DrawText(g,app,font,new Rectangle(2,87,Width-4,24),StudioTheme.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
  using(var font=new Font("Segoe UI",8.5f))TextRenderer.DrawText(g,"Copier et ouvrir",font,new Rectangle(2,112,Width-4,20),StudioTheme.Secondary,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
 }
 protected override void Dispose(bool disposing){if(disposing&&logo!=null)logo.Dispose();base.Dispose(disposing);}
}

public partial class Studio {
 Control CreateSharePanel(Action close){
  var panel=new Panel(){Size=new Size(340,310),BackColor=StudioTheme.Surface,Padding=new Padding(16)};
  panel.Controls.Add(new Label(){Text="Partager l’image",Font=new Font("Segoe UI",12,FontStyle.Bold),ForeColor=StudioTheme.Text,Location=new Point(18,14),Size=new Size(300,28)});
  panel.Controls.Add(new Label(){Text="Copiez, choisissez une conversation, puis Ctrl+V.",Font=new Font("Segoe UI",9),ForeColor=StudioTheme.Secondary,Location=new Point(18,46),Size=new Size(304,38)});
  var row=new FlowLayoutPanel(){Location=new Point(16,84),Size=new Size(308,150),BackColor=StudioTheme.Surface,WrapContents=false};panel.Controls.Add(row);
  var favorites=CaptureHost.preferences.ShareFavorites??new string[0];
  foreach(string name in new[]{"Discord","WhatsApp"})if(Array.IndexOf(favorites,name)>=0){
   string target=name;var tile=new ShareAppTile(target);row.Controls.Add(tile);
   tips.SetToolTip(tile,"Copier l’image et ouvrir "+target+". Choisissez ensuite une conversation et collez avec Ctrl+V.");
   tile.Click+=delegate{close();CopyForFavorite(target);};
  }
  if(row.Controls.Count==0)row.Controls.Add(new Label(){Text="Ajoutez Discord ou WhatsApp dans vos favoris.",ForeColor=StudioTheme.Secondary,Size=new Size(295,70),Padding=new Padding(6,22,0,0)});
  var windows=ModernButton(panel,"Partager avec Windows…","",delegate{close();ShareWithWindows();},300,false);windows.Location=new Point(18,238);windows.Height=36;
  var manage=new LinkLabel(){Text="Choisir les favoris",AutoSize=true,LinkColor=StudioTheme.AccentText,ActiveLinkColor=StudioTheme.AccentText,Location=new Point(20,283)};panel.Controls.Add(manage);manage.LinkClicked+=delegate{close();ChooseShareFavorites();};
  return panel;
 }
 Form CreateOcrDialog(string text){
  var dialog=new Form(){Text="Texte extrait — Capture Studio",Icon=Icon,Width=700,Height=500,MinimumSize=new Size(420,280),StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false,Font=Font,BackColor=StudioTheme.Background};
  dialog.Controls.Add(new TextBox(){Multiline=true,Dock=DockStyle.Fill,Text=text,ScrollBars=ScrollBars.Vertical,BackColor=StudioTheme.Surface,ForeColor=StudioTheme.Text,BorderStyle=BorderStyle.None,Font=Font});
  dialog.Shown+=delegate{try{int value=StudioTheme.Dark?1:0;SetDarkTitlebar(dialog.Handle,20,ref value,4);}catch{}};
  return dialog;
 }
}
