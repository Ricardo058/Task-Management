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
  [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h,uint message,IntPtr w,IntPtr l);
  [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
  [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr h,uint flags);
  [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr h);
  [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadImage(IntPtr instance,string name,uint type,int width,int height,uint flags);
  [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
  static readonly IntPtr Zero=IntPtr.Zero;
  static readonly IntPtr Bottom=new IntPtr(1);

  [STAThread] static void Main(string[] args){
    try{
      string root=AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
      string request=String.Join(" ",args).ToLowerInvariant();
      if(request.Contains("rixu://report")){SaveReport(args,root);return;}
      if(request.Contains("exit-manager")){CloseWindow("Rixu Manager");return;}
      if(request.Contains("close-pocket")){CloseWindow("Rixu Pocket");CloseWindow("Rixu Desktop Widget");return;}
      RegisterProtocol(root);

      bool pocket=request.Contains("pocket"),desktop=request.Contains("desktop"),manage=!pocket&&!desktop;
      string edge=FindEdge();if(edge==null)throw new Exception("Microsoft Edge was not found.");
      string modeQuery=manage?"?manage=1":pocket?"?widget=1&pocket=1":"?widget=1&desktop=1";
      string title=manage?"Rixu Manager":pocket?"Rixu Pocket":"Rixu Desktop Widget";
      string build=GetBuildVersion(root),windowTitle=title+" · "+build;
      string page=new Uri(Path.Combine(root,"web","index.html")).AbsoluteUri+modeQuery+"&build="+build;
      string profile=Path.Combine(root,".widget-profile-v3");
      IntPtr window=FindTitledWindow(windowTitle);
      if(window==Zero){
        if(FindTitledWindow(title)!=Zero){CloseWindow(title);Thread.Sleep(180);}
        string edgeArgs="--app=\""+page+"\" --user-data-dir=\""+profile+"\" --no-first-run --disable-extensions --disable-background-mode "+(manage?"--window-size=1280,820":"--window-size=420,600");
        Process.Start(new ProcessStartInfo(edge,edgeArgs){UseShellExecute=true});
        for(int i=0;i<70&&window==Zero;i++){Thread.Sleep(100);window=FindTitledWindow(windowTitle);}
      }
      if(window==Zero)throw new Exception("The requested Rixu window did not start in time.");

      if(manage){ShowWindow(window,5);SetForegroundWindow(window);HoldSharpWindowIcon(window,root,"Manager");return;}
      var area=Screen.PrimaryScreen.WorkingArea;ShowWindow(window,9);
      if(pocket){int x=area.X+(area.Width-420)/2,y=area.Y+(area.Height-600)/2;SetWindowPos(window,Zero,x,y,420,600,0x0040);SetForegroundWindow(window);HoldSharpWindowIcon(window,root,"Pocket");return;}
      SetParent(window,Zero);SetWindowPos(window,Bottom,area.X+area.Width-442,area.Y+area.Height-622,420,600,0x0040);HoldSharpWindowIcon(window,root,"Desktop");
    }catch(Exception ex){try{File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-error.log"),ex.ToString(),Encoding.UTF8);}catch{}MessageBox.Show(ex.Message,"Rixu",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  }

  static void RegisterProtocol(string root){
    string exe=Process.GetCurrentProcess().MainModule.FileName;
    using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Classes\rixu")){key.SetValue("","URL:Rixu Protocol");key.SetValue("URL Protocol","");using(var icon=key.CreateSubKey("DefaultIcon"))icon.SetValue("",exe+",0");using(var command=key.CreateSubKey(@"shell\open\command"))command.SetValue("","\""+exe+"\" \"%1\"");}
  }
  static void SaveReport(string[] args,string root){
    string raw=Array.Find(args,x=>x.StartsWith("rixu://report",StringComparison.OrdinalIgnoreCase));
    if(String.IsNullOrEmpty(raw))throw new Exception("The report payload is missing.");
    var uri=new Uri(raw);string encoded=null;
    foreach(string part in uri.Query.TrimStart('?').Split('&')){int split=part.IndexOf('=');if(split>0&&part.Substring(0,split)=="data"){encoded=Uri.UnescapeDataString(part.Substring(split+1));break;}}
    if(String.IsNullOrEmpty(encoded))throw new Exception("The report data is missing.");
    encoded=encoded.Replace('-','+').Replace('_','/');while(encoded.Length%4!=0)encoded+="=";
    string markdown=Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
    string reportDir=Path.Combine(root,"\u6267\u884c\u60c5\u51b5");Directory.CreateDirectory(reportDir);
    string fileName=DateTime.Now.ToString("yyyy-MM-dd")+"_\u6267\u884c\u7b80\u62a5.md";
    File.WriteAllText(Path.Combine(reportDir,fileName),markdown,new UTF8Encoding(true));
  }
  static void CloseWindow(string title){for(int i=0;i<12;i++){IntPtr h=FindTitledWindow(title);if(h==Zero)return;PostMessage(h,0x0010,Zero,Zero);Thread.Sleep(120);}}
  static string GetBuildVersion(string root){
    string web=Path.Combine(root,"web");long stamp=0;
    foreach(string name in new[]{"index.html","app.js","styles.css"}){string file=Path.Combine(web,name);if(File.Exists(file))stamp=Math.Max(stamp,File.GetLastWriteTimeUtc(file).Ticks);}
    return stamp.ToString();
  }
  static void HoldSharpWindowIcon(IntPtr window,string root,string mode){
    using(var gate=new Mutex(false,@"Local\RixuSharpIconHost_"+mode)){
      bool owns=false;try{owns=gate.WaitOne(0,false);}catch(AbandonedMutexException){owns=true;}if(!owns)return;
      IntPtr small=Zero,big=Zero;try{
        string path=Path.Combine(root,"assets","rixu-hd.ico");uint dpi=96;try{dpi=GetDpiForWindow(window);if(dpi==0)dpi=96;}catch{dpi=96;}
        int smallSize=Math.Max(16,(int)Math.Round(16*dpi/96d)),bigSize=Math.Max(32,(int)Math.Round(32*dpi/96d));
        small=LoadImage(Zero,path,1,smallSize,smallSize,0x10);big=LoadImage(Zero,path,1,bigSize,bigSize,0x10);
        while(IsWindow(window)){if(small!=Zero){SendMessage(window,0x0080,Zero,small);SendMessage(window,0x0080,new IntPtr(2),small);}if(big!=Zero)SendMessage(window,0x0080,new IntPtr(1),big);Thread.Sleep(1400);}
      }finally{if(small!=Zero)DestroyIcon(small);if(big!=Zero)DestroyIcon(big);try{gate.ReleaseMutex();}catch{}}
    }
  }
  static string FindEdge(){string[] paths={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Microsoft","Edge","Application","msedge.exe"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Microsoft","Edge","Application","msedge.exe")};foreach(string p in paths)if(File.Exists(p))return p;return null;}
  static bool HasTitle(IntPtr h,string wanted){var s=new StringBuilder(256);GetWindowText(h,s,s.Capacity);return s.ToString().Contains(wanted);}
  static IntPtr FindTitledWindow(string wanted){IntPtr found=Zero;EnumWindows((h,p)=>{if(HasTitle(h,wanted)){found=h;return false;}EnumChildWindows(h,(child,cp)=>{if(HasTitle(child,wanted)){found=GetAncestor(child,2);return false;}return true;},Zero);return found==Zero;},Zero);return found;}
}
