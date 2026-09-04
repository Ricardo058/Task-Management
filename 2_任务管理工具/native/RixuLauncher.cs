using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

static class RixuLauncher {
  delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
  [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb,IntPtr p);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent,EnumWindowsProc cb,IntPtr p);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
  [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SetParent(IntPtr child,IntPtr parent);
  [DllImport("user32.dll",SetLastError=true)] static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int hgt,uint flags);
  [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h,int command);
  [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h,uint message,IntPtr w,IntPtr l);
  static readonly IntPtr Zero=IntPtr.Zero;
  static readonly IntPtr Bottom=new IntPtr(1);

  [STAThread] static void Main(string[] args){
    try{
      string root=AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
      RegisterProtocol(root);
      string request=String.Join(" ",args).ToLowerInvariant();
      if(request.Contains("exit-manager")){CloseWindow("Rixu Manager");return;}
      if(request.Contains("close-pocket")){CloseWindow("Rixu Pocket");CloseWindow("Rixu Desktop Widget");return;}

      bool pocket=request.Contains("pocket"),desktop=request.Contains("desktop"),manage=!pocket&&!desktop;
      string edge=FindEdge();if(edge==null)throw new Exception("Microsoft Edge was not found.");
      string modeQuery=manage?"?manage=1":pocket?"?widget=1&pocket=1":"?widget=1&desktop=1";
      string title=manage?"Rixu Manager":pocket?"Rixu Pocket":"Rixu Desktop Widget";
      string page=new Uri(Path.Combine(root,"web","index.html")).AbsoluteUri+modeQuery;
      string profile=Path.Combine(root,".widget-profile-v3");
      IntPtr window=FindTitledWindow(title);
      if(window==Zero){
        string edgeArgs="--app=\""+page+"\" --user-data-dir=\""+profile+"\" --no-first-run "+(manage?"--window-size=1280,820":"--window-size=420,600");
        Process.Start(new ProcessStartInfo(edge,edgeArgs){UseShellExecute=true});
        for(int i=0;i<70&&window==Zero;i++){Thread.Sleep(100);window=FindTitledWindow(title);}
      }
      if(window==Zero)throw new Exception("The requested Rixu window did not start in time.");

      if(manage||pocket){ShowWindow(window,5);SetForegroundWindow(window);return;}
      SetParent(window,Zero);
      var area=Screen.PrimaryScreen.WorkingArea;
      SetWindowPos(window,Bottom,area.X+area.Width-442,area.Y+area.Height-622,420,600,0x0040);
    }catch(Exception ex){try{File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-error.log"),ex.ToString(),Encoding.UTF8);}catch{}MessageBox.Show(ex.Message,"Rixu",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  }

  static void RegisterProtocol(string root){
    string exe=Process.GetCurrentProcess().MainModule.FileName;
    using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Classes\rixu")){key.SetValue("","URL:Rixu Protocol");key.SetValue("URL Protocol","");using(var icon=key.CreateSubKey("DefaultIcon"))icon.SetValue("",exe+",0");using(var command=key.CreateSubKey(@"shell\open\command"))command.SetValue("","\""+exe+"\" \"%1\"");}
  }
  static void CloseWindow(string title){for(int i=0;i<12;i++){IntPtr h=FindTitledWindow(title);if(h==Zero)return;PostMessage(h,0x0010,Zero,Zero);Thread.Sleep(120);}}
  static string FindEdge(){string[] paths={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Microsoft","Edge","Application","msedge.exe"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Microsoft","Edge","Application","msedge.exe")};foreach(string p in paths)if(File.Exists(p))return p;return null;}
  static bool HasTitle(IntPtr h,string wanted){var s=new StringBuilder(256);GetWindowText(h,s,s.Capacity);return s.ToString().Contains(wanted);}
  static IntPtr FindTitledWindow(string wanted){IntPtr found=Zero;EnumWindows((h,p)=>{if(HasTitle(h,wanted)){found=h;return false;}EnumChildWindows(h,(child,cp)=>{if(HasTitle(child,wanted)){found=child;return false;}return true;},Zero);return found==Zero;},Zero);return found;}
}
