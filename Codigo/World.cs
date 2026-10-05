using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
using System.Text;
using System.Windows.Forms;

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
