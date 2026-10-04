using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
using System.Text;
using System.Windows.Forms;

internal sealed class PetPlace : SpriteOverlay
{
    public readonly int Kind;
    public Point Center;
    public int EmptyTicks;
    public bool Dragging;
    readonly Action changed;
    public static readonly string[] Names={"Comedero","Agua","Caja de cartón","Casita","Camita"};
    public PetPlace(int kind,Point at,Action onMove)
    {
        Kind=kind;Center=at;changed=onMove;ClientSize=new Size(68,58);
        MouseDown+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left){Dragging=true;Capture=true;}};
        MouseMove+=delegate{if(Dragging){Center=Cursor.Position;Draw();}};
        MouseUp+=delegate{if(Dragging){Dragging=false;Capture=false;changed();}};
        MouseCaptureChanged+=delegate{if(Dragging){Dragging=false;changed();}};
        Shown+=delegate{SetInteractive(true);Draw();};
    }
    public void Step(){if(EmptyTicks>0 && --EmptyTicks==0)Draw();}
    public void Use(){if(Kind<=1){EmptyTicks=450;Draw();}}
    public void Draw()
    {
        using(var b=new Bitmap(68,58,PixelFormat.Format32bppPArgb))using(Graphics g=Graphics.FromImage(b)){
            g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var pen=new Pen(Color.FromArgb(105,73,56),3)){
                if(Kind<=1){
                    using(var bowl=new SolidBrush(Color.FromArgb(207,116,101))){g.FillEllipse(bowl,8,30,52,20);g.FillRectangle(bowl,12,35,44,12);}
                    g.DrawEllipse(pen,8,29,52,20);
                    if(EmptyTicks==0){
                        using(var fill=new SolidBrush(Kind==0?Color.FromArgb(137,90,53):Color.FromArgb(101,177,221)))g.FillEllipse(fill,14,33,40,11);
                        if(Kind==0){g.FillEllipse(Brushes.Wheat,21,34,5,4);g.FillEllipse(Brushes.Wheat,39,36,5,4);}
                    }
                } else if(Kind==2){
                    using(var fill=new SolidBrush(Color.FromArgb(187,128,70)))g.FillRectangle(fill,6,19,56,37);
                    g.DrawRectangle(pen,6,19,56,37);g.DrawLine(pen,6,19,17,7);g.DrawLine(pen,62,19,51,7);
                    g.DrawArc(pen,22,24,25,23,0,-180);
                } else if(Kind==3){
                    using(var fill=new SolidBrush(Color.FromArgb(232,194,139)))g.FillRectangle(fill,9,22,50,33);
                    using(var roof=new SolidBrush(Color.FromArgb(178,97,80)))g.FillPolygon(roof,new Point[]{new Point(3,24),new Point(34,2),new Point(65,24)});
                    g.FillEllipse(Brushes.SaddleBrown,23,28,23,25);g.DrawPolygon(pen,new Point[]{new Point(3,24),new Point(34,2),new Point(65,24)});
                } else {
                    using(var baseFill=new SolidBrush(Color.FromArgb(163,114,183)))g.FillEllipse(baseFill,4,19,60,35);
                    using(var cushion=new SolidBrush(Color.FromArgb(241,195,220)))g.FillEllipse(cushion,10,22,48,22);
                    g.DrawEllipse(pen,4,19,60,35);
                }
            }
            Present(b,Center.X-34,Center.Y-29);
        }
    }
}

internal static class PetAudio
{
    static readonly List<MemoryStream> streams=new List<MemoryStream>();
    static readonly List<SoundPlayer> players=new List<SoundPlayer>();
    public static void Play(bool purr)
    {
        try {
            const int rate=16000;int count=purr?rate/3:rate/4;
            var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,Encoding.ASCII,true)){
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                for(int i=0;i<count;i++){
                    double t=i/(double)rate, fade=Math.Sin(Math.PI*i/count);
                    double hz=purr?110+18*Math.Sin(2*Math.PI*23*t):650-350*i/(double)count;
                    double wave=Math.Sin(2*Math.PI*hz*t)*(purr?0.22:0.34)*fade;
                    writer.Write((short)(wave*short.MaxValue));
                }
            }
            stream.Position=0;var player=new SoundPlayer(stream);player.Play();streams.Add(stream);players.Add(player);
            if(streams.Count>8){players.RemoveAt(0);streams[0].Dispose();streams.RemoveAt(0);}
        }catch(IOException){}catch(InvalidOperationException){}
    }
}
