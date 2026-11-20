using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

public partial class Studio {
 Button clearCaptureAction;StudioButton captureAction,copyAction,exportAction;Button paletteReference;bool paletteExpanded;
 void RefreshEditorActions(){if(captureAction==null)return;bool ready=photo!=null;captureAction.Primary=!ready;copyAction.Primary=ready;exportAction.Primary=false;copyAction.Enabled=exportAction.Enabled=ready;if(shareAction!=null)shareAction.Enabled=ready;if(clearCaptureAction!=null)clearCaptureAction.Enabled=ready;captureAction.Invalidate();copyAction.Invalidate();exportAction.Invalidate();}
 void RefreshPaletteControls(){if(paletteReference!=null)paletteReference.Visible=paletteExpanded&&paletteSource.SelectedIndex==2;}
 void FitSettingsContent(FlowLayoutPanel panel,FlowLayoutPanel footer){
  bool fitting=false;
  Action reflow=delegate{if(fitting||panel.IsDisposed)return;fitting=true;panel.SuspendLayout();try{
   int available=Math.Max(120,panel.ClientSize.Width-panel.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-5);
   foreach(Control child in panel.Controls){int width=Math.Max(90,available-child.Margin.Horizontal);child.Width=width;
    var label=child as Label;if(label!=null){label.AutoSize=false;label.Height=Math.Max(label.Font.Height+4,TextRenderer.MeasureText(label.Text,label.Font,new Size(width,10000),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl|TextFormatFlags.NoPadding).Height+8);}
    else if(child is CheckBox){((CheckBox)child).AutoSize=false;child.Height=Math.Max(child.Font.Height+12,TextRenderer.MeasureText(child.Text,child.Font,new Size(width-28,10000),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl).Height+10);}
    else if(child is Button)child.Height=Math.Max(40,child.Font.Height+20);
   }
   int remaining=Math.Max(180,footer.ClientSize.Width-footer.Padding.Horizontal-24);foreach(Control child in footer.Controls){child.Width=Math.Min(child.Text=="Annuler"?130:190,remaining/2);child.Height=Math.Max(40,child.Font.Height+20);}footer.Height=Math.Max(76,footer.Font.Height+48);
   panel.HorizontalScroll.Enabled=false;
  }finally{panel.ResumeLayout();fitting=false;}};
  panel.ClientSizeChanged+=delegate{reflow();};panel.FontChanged+=delegate{reflow();};footer.ClientSizeChanged+=delegate{reflow();};
  foreach(Control child in panel.Controls){if(child is Label)child.TextChanged+=delegate{reflow();};child.VisibleChanged+=delegate{reflow();};}
  panel.VisibleChanged+=delegate{reflow();};reflow();
 }
 public static string DefaultAppsUri(bool registered,int windowsBuild){return registered&&windowsBuild>=22000?"ms-settings:defaultapps?registeredAppUser="+Uri.EscapeDataString("CaptureStudio"):"ms-settings:defaultapps";}
 bool HasFileRegistration(){try{using(var key=Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications"))return key!=null&&key.GetValue("CaptureStudio")!=null;}catch{return false;}}
 void OpenDefaultApps(){string uri=DefaultAppsUri(HasFileRegistration(),Environment.OSVersion.Version.Build);try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri){UseShellExecute=true});}catch{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:defaultapps"){UseShellExecute=true});}}
 void AddFileAssociationSettings(FlowLayoutPanel panel,Form dialog){
  Heading(panel,"OUVRIR LES PROJETS .capture AVEC CAPTURE STUDIO");
  var registration=new Label(){ForeColor=StudioTheme.Secondary};panel.Controls.Add(registration);
  Action refresh=delegate{registration.Text=HasFileRegistration()?"Capture Studio est enregistré dans Windows.":"Première étape : enregistrer Capture Studio dans Windows.";};refresh();
  ModernButton(panel,"1. Enregistrer Capture Studio dans Windows","",delegate{RegisterWindowsFiles();refresh();},500,false);
  panel.Controls.Add(new Label(){Text="Dans Applications par défaut, recherchez .capture, sélectionnez Capture Studio et validez. Le choix final reste entre vos mains ; vos applications habituelles pour les photos sont conservées.",ForeColor=StudioTheme.Secondary});
  ModernButton(panel,"2. Choisir l’application pour .capture","",OpenDefaultApps,500,false);
  var hint=new Label(){Text="Sur Windows compatible, le bouton ouvre la fiche de Capture Studio. Sinon, recherchez .capture dans la page générale.",ForeColor=StudioTheme.Secondary};panel.Controls.Add(hint);
  var details=new Label(){Text="Autre possibilité : clic droit sur un projet .capture → Ouvrir avec → Choisir une autre application → Capture Studio → Toujours.\nSi Capture Studio n’est pas proposé, choisissez une application sur votre PC et utilisez le chemin ci-dessous.",ForeColor=StudioTheme.Secondary,Visible=false};panel.Controls.Add(details);
  var path=new TextBox(){ReadOnly=true,Text=System.Reflection.Assembly.GetExecutingAssembly().Location,BackColor=StudioTheme.Surface,ForeColor=StudioTheme.Text,Visible=false};panel.Controls.Add(path);
  var help=ModernButton(panel,"Je ne trouve pas Capture Studio ▾","",delegate{},500,false);bool expanded=false;help.Click+=delegate{expanded=!expanded;details.Visible=path.Visible=expanded;help.Text=expanded?"Masquer l’aide de dépannage ▴":"Je ne trouve pas Capture Studio ▾";};
 }
 void DrawSourceHandle(Graphics g,float x,float y){float r=7/scale;using(var white=new Pen(Color.White,5/scale))using(var color=new Pen(Color.FromArgb(115,87,255),2/scale)){g.DrawLine(white,x-r,y,x+r,y);g.DrawLine(white,x,y-r,x,y+r);g.DrawLine(color,x-r,y,x+r,y);g.DrawLine(color,x,y-r,x,y+r);}}
 void DrawSizeHandle(Graphics g,float x,float y){float r=6/scale;g.FillRectangle(Brushes.White,x-r,y-r,r*2,r*2);using(var pen=new Pen(Color.FromArgb(115,87,255),1.8f/scale))g.DrawRectangle(pen,x-r,y-r,r*2,r*2);}
}
