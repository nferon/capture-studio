using System;
using System.Reflection;
using System.Windows.Forms;
public class ExportShortcutProbe:Studio {
 public int Exports; public int NewWindows;
 protected override void OpenNewWindow(string[] args){NewWindows++;}
 protected override void RunExportShortcut(){Exports++;}
 public bool Dispatch(Keys keys){Message msg=Message.Create(Handle,0x100,(IntPtr)(keys&Keys.KeyCode),IntPtr.Zero);return base.ProcessCmdKey(ref msg,keys);}
 public bool DispatchFrom(Control control,Keys keys){Controls.Add(control);object[] args={Message.Create(control.Handle,0x100,(IntPtr)(keys&Keys.KeyCode),IntPtr.Zero),keys};return (bool)typeof(Control).GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(control,args);}
}
public static class VerifyExportShortcuts {
 [STAThread] public static void Main(){using(var form=new ExportShortcutProbe()){
  if(!form.Dispatch(Keys.Control|Keys.Shift|Keys.S)||form.Exports!=1)throw new Exception("Ctrl+Shift+S non traite");
  if(!form.Dispatch(Keys.Alt|Keys.Shift|Keys.S)||form.Exports!=2)throw new Exception("Alt+Shift+S non traite");
  foreach(Keys keys in new[]{Keys.S,Keys.Alt|Keys.S,Keys.Control|Keys.S,Keys.Control|Keys.Alt|Keys.E}){form.Dispatch(keys);if(form.Exports!=2)throw new Exception("Export declenche par une mauvaise combinaison");}
  foreach(Control control in new Control[]{new TextBox(){Text="Texte conserve"},new Button(),new ComboBox(),new NumericUpDown()}){int before=form.Exports;if(!form.DispatchFrom(control,Keys.Alt|Keys.Shift|Keys.S)||form.Exports!=before+1)throw new Exception("Raccourci perdu depuis "+control.GetType().Name);if(control is TextBox&&control.Text!="Texte conserve")throw new Exception("Texte modifie par le raccourci");}
  if(!form.Dispatch(Keys.Control|Keys.N)||form.NewWindows!=1)throw new Exception("Ctrl+N non traite");
  typeof(Studio).GetMethod("HandleLaunch",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[]{new string[0],true});if(form.NewWindows!=2)throw new Exception("Relancement sans nouvelle fenetre");
  Console.WriteLine("PASS: Ctrl+Shift+S, Alt+Shift+S, absence de faux declenchement, transmission depuis texte/bouton/liste/champ numerique.");
 }}
}
