using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public partial class Studio {
 int activeHotkeyId=CaptureHotkey;uint activeHotkeyKey=0x2c,activeHotkeyModifiers;
 const int HotkeyProbe=7414;
 class ShortcutEditor {public CheckBox Enabled;public ComboBox Mode;public TextBox Keys;public Label Status;public uint Key,Modifiers;public Action Refresh;}
 public static bool IsValidCaptureShortcut(uint key,uint modifiers){
  if((modifiers&~7u)!=0)return false;
  if((modifiers==6&&key==83)||(modifiers==5&&key==83))return false;
  if(key==0x2c&&modifiers==0)return true;
  if(modifiers==2&&(key==65||key==67||key==86||key==88||key==89||key==90||key==83||key==79||key==78||key==80))return false;
  return (modifiers&3)!=0&&((key>=65&&key<=90)||(key>=48&&key<=57)||(key>=112&&key<=135&&key!=123)||key==0x2c);
 }
 public static string CaptureShortcutName(uint key,uint modifiers){return ((modifiers&2)!=0?"Ctrl + ":"")+((modifiers&1)!=0?"Alt + ":"")+((modifiers&4)!=0?"Maj + ":"")+(key==0x2c?"Impr. écran":((Keys)key).ToString());}
 string ShortcutAvailability(uint key,uint modifiers){
  if(!IsValidCaptureShortcut(key,modifiers))return "Choisissez Ctrl et/ou Alt + une lettre, un chiffre ou une touche F (sauf F12).";
  if(hotkeyActive&&activeHotkeyKey==key&&activeHotkeyModifiers==modifiers)return "Actif dans Capture Studio.";
  if(RegisterHotKey(Handle,HotkeyProbe,modifiers|0x4000,key)){UnregisterHotKey(Handle,HotkeyProbe);return "Disponible au moment du test.";}
  int error=Marshal.GetLastWin32Error();return error==1409?"Déjà utilisé par Windows ou un autre logiciel.":"Indisponible (erreur Windows "+error+").";
 }
 bool ApplyCaptureShortcut(bool enabled,uint key,uint modifiers,out string error,bool remember){
  error=null;
  if(enabled&&!IsValidCaptureShortcut(key,modifiers)){error="Ce raccourci n’est pas autorisé. Utilisez Ctrl et/ou Alt avec une lettre, un chiffre ou une touche F (sauf F12).";return false;}
  if(enabled&&!(hotkeyActive&&activeHotkeyKey==key&&activeHotkeyModifiers==modifiers)){
   int next=activeHotkeyId==CaptureHotkey?7413:CaptureHotkey;
   if(!RegisterHotKey(Handle,next,modifiers|0x4000,key)){int code=Marshal.GetLastWin32Error();error=code==1409?"Ce raccourci est déjà utilisé. Choisissez une autre combinaison ou libérez-la dans l’autre logiciel.":"Impossible d’activer ce raccourci (erreur "+code+").";if(hotkeyActive)error+="\nRaccourci conservé : "+CaptureShortcutName(activeHotkeyKey,activeHotkeyModifiers)+".";return false;}
   if(hotkeyActive)UnregisterHotKey(Handle,activeHotkeyId);
   activeHotkeyId=next;activeHotkeyKey=key;activeHotkeyModifiers=modifiers;hotkeyActive=true;
  }else if(!enabled&&hotkeyActive){UnregisterHotKey(Handle,activeHotkeyId);hotkeyActive=false;}
  hotkeyMenu.Checked=hotkeyActive;
  if(remember){preferences.Hotkey=enabled;preferences.HotkeyKey=key;preferences.HotkeyModifiers=modifiers;SavePreferences();}
  if(hotkeyActive)EnsureTray();
  status.Text=hotkeyActive?"Raccourci actif : "+CaptureShortcutName(activeHotkeyKey,activeHotkeyModifiers):"Raccourci global désactivé.";
  return true;
 }
 ShortcutEditor AddShortcutSettings(FlowLayoutPanel panel){
  Heading(panel,"RACCOURCI DE CAPTURE");var editor=new ShortcutEditor();
  editor.Enabled=new CheckBox(){Text="Activer un raccourci global de capture",Checked=preferences.Hotkey||hotkeyActive,AutoSize=true,ForeColor=StudioTheme.Text,Margin=new Padding(3,8,3,8)};panel.Controls.Add(editor.Enabled);
  editor.Key=preferences.HotkeyKey;editor.Modifiers=preferences.HotkeyModifiers;
  if(!IsValidCaptureShortcut(editor.Key,editor.Modifiers)){editor.Key=0x2c;editor.Modifiers=0;}
  editor.Mode=new ComboBox(){DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,Width=500,BackColor=StudioTheme.Surface,ForeColor=StudioTheme.Text};editor.Mode.Items.AddRange(new object[]{"Utiliser Impr. écran","Utiliser un raccourci personnalisé"});panel.Controls.Add(editor.Mode);
  editor.Keys=new TextBox(){ReadOnly=true,Width=500,BackColor=StudioTheme.Surface,ForeColor=StudioTheme.Text};panel.Controls.Add(editor.Keys);tips.SetToolTip(editor.Keys,"Cliquez ici puis appuyez sur la combinaison souhaitée, par exemple Ctrl + Maj + S.");
  editor.Status=new Label(){Width=520,Height=66,ForeColor=StudioTheme.Secondary,Margin=new Padding(3,8,3,0)};panel.Controls.Add(editor.Status);
  editor.Refresh=delegate{bool custom=editor.Mode.SelectedIndex==1;editor.Keys.Visible=custom;editor.Keys.Enabled=custom&&editor.Enabled.Checked;editor.Keys.Text=CaptureShortcutName(editor.Key,editor.Modifiers);string current=hotkeyActive?"Actuellement actif : "+CaptureShortcutName(activeHotkeyKey,activeHotkeyModifiers)+".":"Actuellement : raccourci désactivé.";bool same=hotkeyActive&&editor.Key==activeHotkeyKey&&editor.Modifiers==activeHotkeyModifiers;if(!editor.Enabled.Checked)current+=hotkeyActive?"\nL’enregistrement désactivera la capture par raccourci.":"\nCochez la case pour activer le raccourci choisi.";else if(!same)current+="\nNouveau choix : "+CaptureShortcutName(editor.Key,editor.Modifiers)+" — "+ShortcutAvailability(editor.Key,editor.Modifiers);editor.Status.Text=current;};
  editor.Enabled.CheckedChanged+=delegate{editor.Refresh();};
  editor.Mode.SelectedIndexChanged+=delegate{if(editor.Mode.SelectedIndex==0){editor.Key=0x2c;editor.Modifiers=0;}else if(editor.Key==0x2c&&editor.Modifiers==0){editor.Key=(uint)System.Windows.Forms.Keys.S;editor.Modifiers=6;}editor.Refresh();};
  editor.Keys.KeyDown+=delegate(object sender,KeyEventArgs e){e.SuppressKeyPress=true;if(e.KeyCode==System.Windows.Forms.Keys.Escape||e.KeyCode==System.Windows.Forms.Keys.ControlKey||e.KeyCode==System.Windows.Forms.Keys.ShiftKey||e.KeyCode==System.Windows.Forms.Keys.Menu)return;uint mods=(e.Control?2u:0)|(e.Alt?1u:0)|(e.Shift?4u:0);uint key=(uint)e.KeyCode;if(!IsValidCaptureShortcut(key,mods)){editor.Status.Text="Combinaison non prise en charge : utilisez Ctrl et/ou Alt avec une lettre, un chiffre ou une touche F (sauf F12).";return;}editor.Key=key;editor.Modifiers=mods;editor.Refresh();};
  editor.Mode.SelectedIndex=editor.Key==0x2c&&editor.Modifiers==0?0:1;
  ModernButton(panel,"Vérifier la disponibilité maintenant","",delegate{editor.Refresh();editor.Status.Text+="\nTest ponctuel : "+CaptureShortcutName(editor.Key,editor.Modifiers)+" — "+ShortcutAvailability(editor.Key,editor.Modifiers);},500,false);
  var keyboardHelp=new Label(){ForeColor=StudioTheme.Secondary,Text="Impr. écran ouvre l’outil Microsoft ? Désactivez son option dans les réglages clavier Windows. Pour un autre logiciel, libérez la touche dans ses préférences ou utilisez une combinaison personnalisée. Aucun réglage de l’autre logiciel n’est modifié."};panel.Controls.Add(keyboardHelp);
  var keyboardLink=ModernButton(panel,"Réglages clavier Windows pour Impr. écran","",delegate{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:easeofaccess-keyboard"){UseShellExecute=true});},500,false);
  Action contextualHelp=delegate{keyboardHelp.Visible=keyboardLink.Visible=editor.Mode.SelectedIndex==0;};editor.Mode.SelectedIndexChanged+=delegate{contextualHelp();};contextualHelp();
  editor.Refresh();return editor;
 }
}
