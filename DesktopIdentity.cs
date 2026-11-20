using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
public partial class Studio {
 public const string ApplicationId="Botaneo.CaptureStudio.Desktop";
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern int SetCurrentProcessExplicitAppUserModelID(string id);
 static void SetDesktopIdentity(){SetCurrentProcessExplicitAppUserModelID(ApplicationId);}
 void ApplyApplicationIcon(){string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"CaptureStudio.ico");if(File.Exists(path))Icon=new Icon(path);else{var icon=Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);if(icon!=null)Icon=icon;}}
 [StructLayout(LayoutKind.Sequential)] struct PropertyKey {public Guid Format;public uint Id;}
 [StructLayout(LayoutKind.Explicit,Size=24)] struct PropertyValue {[FieldOffset(0)]public ushort Type;[FieldOffset(8)]public IntPtr Text;}
 [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IPropertyStore {
  void GetCount(out uint count);void GetAt(uint index,out PropertyKey key);void GetValue(ref PropertyKey key,out PropertyValue value);void SetValue(ref PropertyKey key,ref PropertyValue value);void Commit();
 }
 public static void StampShortcut(string path){object link=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046")));IntPtr text=IntPtr.Zero;try{var file=(IPersistFile)link;file.Load(path,2);var store=(IPropertyStore)link;var key=new PropertyKey(){Format=new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),Id=5};text=Marshal.StringToCoTaskMemUni(ApplicationId);var value=new PropertyValue(){Type=31,Text=text};store.SetValue(ref key,ref value);store.Commit();file.Save(path,true);}finally{if(text!=IntPtr.Zero)Marshal.FreeCoTaskMem(text);Marshal.FinalReleaseComObject(link);}}
}
