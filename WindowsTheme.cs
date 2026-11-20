using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

static class StudioTheme {
 public static bool Dark;
 public static Color Surface {get{return Dark?Color.FromArgb(39,42,53):Color.White;}}
 public static Color Background {get{return Dark?Color.FromArgb(27,30,39):Color.FromArgb(244,245,249);}}
 public static Color Canvas {get{return Dark?Color.FromArgb(21,24,32):Color.FromArgb(239,241,246);}}
 public static Color Text {get{return Dark?Color.FromArgb(232,234,242):Color.FromArgb(54,61,78);}}
 public static Color Secondary {get{return Dark?Color.FromArgb(175,182,202):Color.FromArgb(92,101,120);}}
 public static Color Hover {get{return Dark?Color.FromArgb(58,62,78):Color.FromArgb(242,244,249);}}
 public static Color Active {get{return Dark?Color.FromArgb(69,53,111):Color.FromArgb(238,233,255);}}
 public static Color AccentText {get{return Dark?Color.FromArgb(201,185,255):Color.FromArgb(104,73,219);}}
 public static Color Track {get{return Dark?Color.FromArgb(73,78,96):Color.FromArgb(232,234,242);}}
}
public partial class Studio {
 Dictionary<Control,Color> themeOriginalBack=new Dictionary<Control,Color>(),themeOriginalFore=new Dictionary<Control,Color>();
 [DllImport("dwmapi.dll",EntryPoint="DwmSetWindowAttribute")]static extern int SetDarkTitlebar(IntPtr window,int attribute,ref int value,int size);
 void ApplyWindowsTheme(){
  bool dark=preferences.Theme=="Sombre";if(preferences.Theme!="Clair"&&preferences.Theme!="Sombre")try{using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))dark=key!=null&&Convert.ToInt32(key.GetValue("AppsUseLightTheme",1))==0;}catch{}
  if(SystemInformation.HighContrast)dark=false;StudioTheme.Dark=dark;ThemeControl(this);canvas.BackColor=StudioTheme.Canvas;BackColor=StudioTheme.Background;try{int value=dark?1:0;SetDarkTitlebar(Handle,20,ref value,4);}catch{}Invalidate(true);
 }
 void ThemeControl(Control control){
  if(!themeOriginalBack.ContainsKey(control)){themeOriginalBack[control]=control.BackColor;themeOriginalFore[control]=control.ForeColor;}Color original=themeOriginalBack[control];bool swatch=control is StudioButton&&control.Text.Length==0;
  if(!swatch){if(original.ToArgb()==Color.White.ToArgb())control.BackColor=StudioTheme.Surface;else if(original.ToArgb()==Color.FromArgb(244,245,249).ToArgb())control.BackColor=StudioTheme.Background;else if(original.ToArgb()==Color.FromArgb(239,241,246).ToArgb())control.BackColor=StudioTheme.Canvas;
   control.ForeColor=control.Font.Bold?StudioTheme.Text:StudioTheme.Secondary;
   if(control is ComboBox||control is TextBoxBase){control.BackColor=StudioTheme.Surface;control.ForeColor=StudioTheme.Text;}
  }
  foreach(Control child in control.Controls)ThemeControl(child);control.Invalidate();
 }
}
