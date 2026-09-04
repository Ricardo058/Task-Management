using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class IconGenerator {
  static GraphicsPath RoundRect(RectangleF r, float radius) {
    var p = new GraphicsPath(); float d = radius * 2;
    p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right-d, r.Y, d, d, 270, 90);
    p.AddArc(r.Right-d, r.Bottom-d, d, d, 0, 90); p.AddArc(r.X, r.Bottom-d, d, d, 90, 90); p.CloseFigure(); return p;
  }
  static byte[] Draw(int size) {
    using(var bmp=new Bitmap(size,size,PixelFormat.Format32bppArgb)) using(var g=Graphics.FromImage(bmp)) {
      g.SmoothingMode=SmoothingMode.AntiAlias; g.Clear(Color.Transparent); float s=size/128f;
      using(var path=RoundRect(new RectangleF(8*s,8*s,112*s,112*s),34*s)) using(var brush=new LinearGradientBrush(new PointF(10*s,10*s),new PointF(118*s,118*s),Color.FromArgb(53,116,93),Color.FromArgb(23,73,55))) g.FillPath(brush,path);
      using(var amber=new SolidBrush(Color.FromArgb(245,189,106))) g.FillEllipse(amber,84*s,26*s,16*s,16*s);
      using(var pen=new Pen(Color.FromArgb(255,250,240),9*s)){pen.StartCap=pen.EndCap=LineCap.Round;g.DrawBezier(pen,37*s,81*s,49*s,80*s,59*s,70*s,64*s,59*s);g.DrawBezier(pen,58*s,87*s,71*s,84*s,82*s,69*s,87*s,56*s);g.DrawBezier(pen,32*s,61*s,40*s,61*s,46*s,56*s,50*s,49*s);}
      using(var pen=new Pen(Color.FromArgb(157,206,177),5*s)){pen.StartCap=pen.EndCap=LineCap.Round;g.DrawBezier(pen,33*s,91*s,53*s,98*s,78*s,96*s,96*s,86*s);}
      using(var ms=new MemoryStream()){bmp.Save(ms,ImageFormat.Png);return ms.ToArray();}
    }
  }
  static void Main(string[] args) {
    string root=args.Length>0?args[0]:Directory.GetCurrentDirectory(),assets=Path.Combine(root,"assets");Directory.CreateDirectory(assets);
    File.WriteAllBytes(Path.Combine(assets,"icon-192.png"),Draw(192));File.WriteAllBytes(Path.Combine(assets,"icon-512.png"),Draw(512));
    int[] sizes={16,24,32,48,64,128,256};var images=new List<byte[]>();foreach(int s in sizes)images.Add(Draw(s));
    using(var fs=File.Create(Path.Combine(assets,"rixu.ico"))) using(var w=new BinaryWriter(fs)){w.Write((ushort)0);w.Write((ushort)1);w.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)0);w.Write((byte)0);w.Write((ushort)1);w.Write((ushort)32);w.Write(images[i].Length);w.Write(offset);offset+=images[i].Length;}foreach(var bytes in images)w.Write(bytes);}
  }
}
