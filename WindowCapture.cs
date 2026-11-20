using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

public class CaptureWindow {
 public IntPtr Handle; public Rectangle Bounds; public string Title;
}
public static class WindowCapture {
 [StructLayout(LayoutKind.Sequential)] struct Rect {public int Left,Top,Right,Bottom; public Rectangle Value {get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);}}}
 delegate bool EnumProc(IntPtr h,IntPtr p);
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback,IntPtr param);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr64(IntPtr window,int index);
 [DllImport("user32.dll",EntryPoint="GetWindowLongW")] static extern int GetWindowLong32(IntPtr window,int index);
 public static bool IsSelectableStyle(long style){return (style&(0x20L|0x08000000L))==0;}
 static bool IsInteractiveWindow(IntPtr window){long style=IntPtr.Size==8?GetWindowLongPtr64(window,-20).ToInt64():GetWindowLong32(window,-20);return IsSelectableStyle(style); }
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out Rect rect);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder title,int count);
 [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h,int attribute,out Rect value,int size);
 [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] static extern int GetCloaked(IntPtr h,int attribute,out int value,int size);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr context);
 [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
 public static void EnableDpi(){try{if(SetProcessDpiAwarenessContext(new IntPtr(-4)))return;}catch(EntryPointNotFoundException){}SetProcessDPIAware();}
 public static Rectangle BoundsFor(IntPtr h){Rect rect;if(!IsWindowVisible(h)||IsIconic(h))throw new InvalidOperationException("La fenêtre a été fermée ou réduite. Relancez la capture.");if(DwmGetWindowAttribute(h,9,out rect,16)!=0&&!GetWindowRect(h,out rect))throw new InvalidOperationException("Impossible de mesurer cette fenêtre.");if(rect.Value.Width<1||rect.Value.Height<1)throw new InvalidOperationException("Dimensions de fenêtre non valides.");return rect.Value;}
 public static List<CaptureWindow> List(IntPtr excluded){var result=new List<CaptureWindow>();EnumWindows(delegate(IntPtr h,IntPtr p){if(h==excluded||!IsWindowVisible(h)||IsIconic(h)||!IsInteractiveWindow(h))return true;int cloaked;if(GetCloaked(h,14,out cloaked,4)==0&&cloaked!=0)return true;var title=new StringBuilder(1024);GetWindowText(h,title,title.Capacity);if(title.Length==0)return true;try{var r=BoundsFor(h);if(r.Width>20&&r.Height>20&&r.IntersectsWith(SystemInformation.VirtualScreen))result.Add(new CaptureWindow(){Handle=h,Bounds=r,Title=title.ToString()});}catch(InvalidOperationException){}return true;},IntPtr.Zero);return result;}
 public static CaptureWindow At(List<CaptureWindow> windows,Point point){foreach(var w in windows)if(w.Bounds.Contains(point))return w;return null;}
 public static Rectangle ScreenBounds(Rectangle window,Rectangle screen){var visible=Rectangle.Intersect(window,screen);if(visible.Width<=0||visible.Height<=0)throw new InvalidOperationException("La fenêtre se trouve en dehors des écrans.");return visible;}
}
public class WindowPicker:Form {
 readonly Bitmap image;readonly Rectangle desktop;readonly List<CaptureWindow> windows;CaptureWindow hovered;readonly int delaySeconds;
 public string TimingInstruction {get{return delaySeconds>0?"Le retardateur de "+delaySeconds+" seconde"+(delaySeconds>1?"s":"")+" démarre APRÈS votre clic.":"La capture démarre après votre clic (sans retardateur).";}}
 public CaptureWindow SelectedWindow {get;private set;}
 public WindowPicker(Bitmap screenshot,Rectangle screen,List<CaptureWindow> candidates):this(screenshot,screen,candidates,0){}
 public WindowPicker(Bitmap screenshot,Rectangle screen,List<CaptureWindow> candidates,int seconds){delaySeconds=seconds;image=screenshot;desktop=screen;windows=candidates;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=screen;TopMost=true;DoubleBuffered=true;Cursor=Cursors.Hand;KeyPreview=true;ShowInTaskbar=false;MouseMove+=delegate(object s,MouseEventArgs e){hovered=WindowCapture.At(windows,new Point(e.X+desktop.X,e.Y+desktop.Y));Invalidate();};MouseDown+=delegate(object s,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;hovered=WindowCapture.At(windows,new Point(e.X+desktop.X,e.Y+desktop.Y));if(hovered!=null){SelectedWindow=hovered;DialogResult=DialogResult.OK;Close();}};KeyDown+=delegate(object s,KeyEventArgs e){if(e.KeyCode==Keys.Escape){DialogResult=DialogResult.Cancel;Close();}};}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.DrawImageUnscaled(image,0,0);using(var shade=new SolidBrush(Color.FromArgb(115,0,0,0)))g.FillRectangle(shade,ClientRectangle);if(hovered!=null){var visible=WindowCapture.ScreenBounds(hovered.Bounds,desktop);visible.Offset(-desktop.X,-desktop.Y);g.DrawImage(image,visible,visible,GraphicsUnit.Pixel);using(var border=new Pen(Color.FromArgb(115,87,255),4))g.DrawRectangle(border,visible.X+2,visible.Y+2,Math.Max(1,visible.Width-4),Math.Max(1,visible.Height-4));}using(var font=new Font("Segoe UI",13,FontStyle.Bold)){var label=new Rectangle(20,20,Math.Min(1100,ClientSize.Width-40),104);g.FillRectangle(Brushes.White,label);g.DrawString((hovered==null?"Cliquez sur la fenêtre à capturer.":hovered.Title+" — "+hovered.Bounds.Width+" × "+hovered.Bounds.Height+" pixels")+"\n"+TimingInstruction+"\nÉchap : annuler",font,Brushes.Black,label);}}
}
