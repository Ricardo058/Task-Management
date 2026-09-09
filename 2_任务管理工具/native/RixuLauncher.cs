using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
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
  static readonly IntPtr TopMost=new IntPtr(-1);
  static readonly IntPtr NoTopMost=new IntPtr(-2);

  [STAThread] static void Main(string[] args){
    try{
      string root=AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
      string request=String.Join(" ",args).ToLowerInvariant();
      if(request.Contains("rixu://report")){SaveReport(args,root);return;}
      if(request.Contains("rixu://topmost")){SetTopmost(request);return;}
      if(request.Contains("rixu://wechat")){OpenWechat();return;}
      if(request.Contains("exit-manager")){CloseWindow("Rixu Manager");return;}
      if(request.Contains("close-pocket")){CloseWindow("Rixu Pocket");CloseWindow("Rixu Desktop Widget");return;}
      try{RegisterProtocol(root);}catch{}

      bool pocket=request.Contains("pocket"),desktop=request.Contains("desktop"),manage=!pocket&&!desktop;
      string edge=FindEdge();if(edge==null)throw new Exception("Microsoft Edge was not found.");
      var stateServer=new StateServer(root);stateServer.Start();
      string modeQuery=manage?"?manage=1":pocket?"?widget=1&pocket=1":"?widget=1&desktop=1&topmost=1";
      string title=manage?"Rixu Manager":pocket?"Rixu Pocket":"Rixu Desktop Widget";
      string build=GetBuildVersion(root),windowTitle=title+" · "+build;
      string page=new Uri(Path.Combine(root,"web","index.html")).AbsoluteUri+modeQuery+"&build="+build+"&apiPort="+stateServer.Port;
      string profile=PrepareProfile(root);
      IntPtr window=FindTitledWindow(windowTitle);
      if(window==Zero){
        if(FindTitledWindow(title)!=Zero){CloseWindow(title);Thread.Sleep(180);}
        string edgeArgs="--app=\""+page+"\" --user-data-dir=\""+profile+"\" --no-first-run --no-default-browser-check --disable-extensions --disable-background-mode --disable-sync "+(manage?"--window-size=1280,820":"--window-size=420,600");
        Process.Start(new ProcessStartInfo(edge,edgeArgs){UseShellExecute=true});
        for(int i=0;i<70&&window==Zero;i++){Thread.Sleep(100);window=FindTitledWindow(windowTitle);}
      }
      if(window==Zero)throw new Exception("The requested Rixu window did not start in time.");

      if(manage){ShowWindow(window,5);SetForegroundWindow(window);HoldSharpWindowIcon(window,root,"Manager");return;}
      var area=Screen.PrimaryScreen.WorkingArea;ShowWindow(window,9);
      if(pocket){int x=area.X+(area.Width-420)/2,y=area.Y+(area.Height-600)/2;SetWindowPos(window,Zero,x,y,420,600,0x0040);SetForegroundWindow(window);HoldSharpWindowIcon(window,root,"Pocket");return;}
      SetParent(window,Zero);SetWindowPos(window,TopMost,area.X+area.Width-442,area.Y+area.Height-622,420,600,0x0040);SetForegroundWindow(window);HoldSharpWindowIcon(window,root,"Desktop");
    }catch(Exception ex){try{File.WriteAllText(Path.Combine(RuntimeRoot(),"launcher-error.log"),ex.ToString(),Encoding.UTF8);}catch{}MessageBox.Show(ex.Message,"Rixu",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  }

  static void RegisterProtocol(string root){
    string exe=Process.GetCurrentProcess().MainModule.FileName;
    using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Classes\rixu")){key.SetValue("","URL:Rixu Protocol");key.SetValue("URL Protocol","");using(var icon=key.CreateSubKey("DefaultIcon"))icon.SetValue("",exe+",0");using(var command=key.CreateSubKey(@"shell\open\command"))command.SetValue("","\""+exe+"\" \"%1\"");}
  }
  static string RuntimeRoot(){string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RixuPlanner");Directory.CreateDirectory(path);return path;}
  static string PrepareProfile(string root){
    string profile=Path.Combine(RuntimeRoot(),"EdgeProfile"),target=Path.Combine(profile,"Default","Local Storage","leveldb");
    if(!Directory.Exists(target)){
      string source=Path.Combine(root,".widget-profile-v3","Default","Local Storage","leveldb");
      if(Directory.Exists(source))CopyDirectory(source,target);else Directory.CreateDirectory(profile);
    }
    return profile;
  }
  static void CopyDirectory(string source,string target){
    Directory.CreateDirectory(target);
    foreach(string file in Directory.GetFiles(source))File.Copy(file,Path.Combine(target,Path.GetFileName(file)),true);
    foreach(string dir in Directory.GetDirectories(source))CopyDirectory(dir,Path.Combine(target,Path.GetFileName(dir)));
  }
  static void SetTopmost(string request){
    bool enabled=request.Contains("enabled=1"),desktop=request.Contains("mode=desktop");
    string title=desktop?"Rixu Desktop Widget":"Rixu Pocket";IntPtr window=FindTitledWindow(title);
    if(window==Zero)window=FindTitledWindow(desktop?"Rixu Pocket":"Rixu Desktop Widget");
    if(window==Zero)return;
    SetWindowPos(window,enabled?TopMost:NoTopMost,0,0,0,0,0x0013);
    if(enabled)SetForegroundWindow(window);
  }
  static void OpenWechat(){
    string runningPath=null;
    foreach(string name in new[]{"Weixin","WeChat"})foreach(Process process in Process.GetProcessesByName(name)){if(process.MainWindowHandle!=Zero){ShowWindow(process.MainWindowHandle,9);SetForegroundWindow(process.MainWindowHandle);return;}try{if(String.IsNullOrEmpty(runningPath))runningPath=process.MainModule.FileName;}catch{}}
    if(!String.IsNullOrEmpty(runningPath)&&File.Exists(runningPath)){Process.Start(new ProcessStartInfo(runningPath){UseShellExecute=true});return;}
    try{Process.Start(new ProcessStartInfo("weixin://"){UseShellExecute=true});return;}catch{}
    string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),programs=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),programsX86=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    foreach(string path in new[]{Path.Combine(programsX86,"Tencent","WeChat","WeChat.exe"),Path.Combine(programs,"Tencent","Weixin","Weixin.exe"),Path.Combine(local,"Tencent","WeChat","WeChat.exe")})if(File.Exists(path)){Process.Start(new ProcessStartInfo(path){UseShellExecute=true});return;}
    throw new Exception("未找到微信。图片已保存在下载文件夹，可在微信中手动选择发送。");
  }
  static void WriteAtomic(string path,string content){
    string temp=path+".tmp";File.WriteAllText(temp,content,new UTF8Encoding(false));
    try{if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}catch{File.Copy(temp,path,true);File.Delete(temp);}
  }
  static void SaveProjectState(string root,string json){
    if(!json.TrimStart().StartsWith("{")||!json.TrimEnd().EndsWith("}"))throw new Exception("The state payload is invalid.");
    string directory=Path.Combine(root,"data");Directory.CreateDirectory(directory);WriteAtomic(Path.Combine(directory,"rixu-data.json"),json);WriteAtomic(Path.Combine(directory,"rixu-data.js"),"window.__RIXU_PROJECT_DATA__="+json+";");
  }
  sealed class StateServer {
    readonly string root;TcpListener listener;public int Port{get;private set;}
    public StateServer(string rootPath){root=rootPath;}
    public void Start(){listener=new TcpListener(IPAddress.Loopback,0);listener.Start();Port=((IPEndPoint)listener.LocalEndpoint).Port;var thread=new Thread(Listen){IsBackground=true,Name="RixuStateServer"};thread.Start();}
    void Listen(){while(true){try{var client=listener.AcceptTcpClient();ThreadPool.QueueUserWorkItem(Handle,client);}catch{return;}}}
    void Handle(object state){
      using(var client=(TcpClient)state)try{
        var stream=client.GetStream();stream.ReadTimeout=5000;byte[] chunk=new byte[4096];using(var request=new MemoryStream()){
          int headerEnd=-1,read;byte[] all=null;
          while(headerEnd<0&&request.Length<65536&&(read=stream.Read(chunk,0,chunk.Length))>0){request.Write(chunk,0,read);all=request.ToArray();headerEnd=FindHeaderEnd(all,all.Length);}
          if(headerEnd<0)throw new Exception("Invalid request headers.");
          string header=Encoding.ASCII.GetString(all,0,headerEnd),first=header.Split(new[]{"\r\n"},StringSplitOptions.None)[0];
          if(first.StartsWith("OPTIONS ",StringComparison.OrdinalIgnoreCase)){Respond(stream,"204 No Content");return;}
          if(!first.StartsWith("POST /state ",StringComparison.OrdinalIgnoreCase)){Respond(stream,"404 Not Found");return;}
          int length=ContentLength(header);if(length<2||length>4*1024*1024)throw new Exception("Invalid content length.");int bodyStart=headerEnd+4;
          while(request.Length<bodyStart+length&&(read=stream.Read(chunk,0,Math.Min(chunk.Length,(int)(bodyStart+length-request.Length))))>0)request.Write(chunk,0,read);
          all=request.ToArray();if(all.Length<bodyStart+length)throw new Exception("Incomplete request body.");SaveProjectState(root,Encoding.UTF8.GetString(all,bodyStart,length));Respond(stream,"204 No Content");
        }
      }catch{try{Respond(client.GetStream(),"400 Bad Request");}catch{}}
    }
    static int FindHeaderEnd(byte[] bytes,int length){for(int i=3;i<length;i++)if(bytes[i-3]==13&&bytes[i-2]==10&&bytes[i-1]==13&&bytes[i]==10)return i-3;return-1;}
    static int ContentLength(string header){foreach(string line in header.Split(new[]{"\r\n"},StringSplitOptions.None))if(line.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase)){int value;if(Int32.TryParse(line.Substring(15).Trim(),out value))return value;}return 0;}
    static void Respond(NetworkStream stream,string status){byte[] response=Encoding.ASCII.GetBytes("HTTP/1.1 "+status+"\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Allow-Methods: POST, OPTIONS\r\nAccess-Control-Allow-Headers: Content-Type\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");stream.Write(response,0,response.Length);}
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
