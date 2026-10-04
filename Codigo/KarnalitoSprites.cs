using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

internal static class KarnalitoSprites
{
    public const int CellSize = 128;
    public static Bitmap Load()
    {
        using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("karnalito.png")) {
            if(stream==null)throw new FileNotFoundException("Faltan los sprites del Karnalito.");
            using(var source=new Bitmap(stream))return Build(source);
        }
    }
    public static Bitmap Build(Bitmap source)
    {
        // Destination coordinates match the original oneko animation table.
        // Source is the user's 8 x 4 sheet. Mirror only when facing left.
        int[][] mapping = {
            new[]{0,0,6,3,0}, new[]{0,1,7,3,0},
            new[]{1,0,2,1,1}, new[]{1,1,3,1,1},
            new[]{2,0,3,2,0}, new[]{2,1,5,2,0},
            new[]{3,0,3,0,0}, new[]{3,1,4,0,0},
            new[]{4,0,6,1,1}, new[]{4,1,7,2,1},
            new[]{5,0,6,1,0}, new[]{6,0,6,2,0}, new[]{7,0,7,2,0},
            new[]{5,1,5,0,0}, new[]{5,2,6,0,0},
            new[]{6,1,6,0,1}, new[]{5,3,5,0,1},
            new[]{7,1,6,2,0},
            new[]{0,2,2,1,0}, new[]{0,3,3,1,0},
            new[]{1,2,2,1,0}, new[]{1,3,7,1,0},
            new[]{2,2,6,1,0}, new[]{2,3,7,2,0},
            new[]{3,2,1,2,0}, new[]{3,3,0,0,0},
            new[]{4,2,3,0,1}, new[]{4,3,4,0,1},
            new[]{6,2,7,2,0}, new[]{6,3,2,0,0},
            new[]{7,2,7,0,0}, new[]{7,3,0,2,0}
        };
        var atlas=new Bitmap(CellSize*8,CellSize*4,PixelFormat.Format32bppArgb);
        using(Graphics g=Graphics.FromImage(atlas)) {
            g.CompositingMode=CompositingMode.SourceCopy;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            foreach(int[] m in mapping) {
                int left=m[2]*source.Width/8,top=m[3]*source.Height/4;
                int right=(m[2]+1)*source.Width/8,bottom=(m[3]+1)*source.Height/4;
                Rectangle occupied=Occupied(source,new Rectangle(left,top,right-left,bottom-top));
                using(Bitmap crop=source.Clone(occupied,PixelFormat.Format32bppArgb)) {
                    // The tired source cell contains a tiny fragment of its neighbour.
                    if(m[2]==1 && m[3]==2)KeepLargestComponent(crop);
                    if(m[4]!=0)crop.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    // Common scale and bottom alignment preserve the short sleeping pose.
                    double factor=(CellSize-12)/(double)Math.Max(source.Width/8,source.Height/4);
                    int w=Math.Max(1,(int)Math.Round(crop.Width*factor));
                    int h=Math.Max(1,(int)Math.Round(crop.Height*factor));
                    int x=m[0]*CellSize+(CellSize-w)/2,y=m[1]*CellSize+CellSize-6-h;
                    g.DrawImage(crop,new Rectangle(x,y,w,h),0,0,crop.Width,crop.Height,GraphicsUnit.Pixel);
                }
            }
        }
        return atlas;
    }
    static void KeepLargestComponent(Bitmap image)
    {
        int w=image.Width,h=image.Height;
        var seen=new bool[w*h];
        var largest=new System.Collections.Generic.List<int>();
        for(int start=0;start<w*h;start++) {
            if(seen[start] || image.GetPixel(start%w,start/w).A<8)continue;
            var pixels=new System.Collections.Generic.List<int>();
            var queue=new System.Collections.Generic.Queue<int>();queue.Enqueue(start);seen[start]=true;
            while(queue.Count>0) {
                int p=queue.Dequeue(),x=p%w,y=p/w;pixels.Add(p);
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++) {
                    int nx=x+dx,ny=y+dy;
                    if(nx<0 || ny<0 || nx>=w || ny>=h)continue;
                    int next=ny*w+nx;
                    if(!seen[next] && image.GetPixel(nx,ny).A>=8){seen[next]=true;queue.Enqueue(next);}
                }
            }
            if(pixels.Count>largest.Count)largest=pixels;
        }
        var keep=new bool[w*h];foreach(int p in largest)keep[p]=true;
        for(int p=0;p<w*h;p++)if(!keep[p])image.SetPixel(p%w,p/w,Color.Transparent);
    }
    static Rectangle Occupied(Bitmap source,Rectangle cell)
    {
        int left=cell.Right,top=cell.Bottom,right=cell.Left,bottom=cell.Top;
        for(int y=cell.Top;y<cell.Bottom;y++)for(int x=cell.Left;x<cell.Right;x++) {
            if(source.GetPixel(x,y).A<12)continue;
            left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);
        }
        if(right<left || bottom<top)throw new InvalidDataException("Una pose del Karnalito está vacía.");
        return Rectangle.FromLTRB(left,top,right+1,bottom+1);
    }
}
