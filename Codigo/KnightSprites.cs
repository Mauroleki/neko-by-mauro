using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

internal static class KnightSprites
{
    public const int CellSize=128;
    const int SourceSize=80;

    public static Bitmap Load()
    {
        using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("knight.png")) {
            if(stream==null)throw new FileNotFoundException("Faltan los sprites del Caballero.");
            using(var source=new Bitmap(stream))return Build(source);
        }
    }

    public static Bitmap Build(Bitmap source)
    {
        if(source.Width<SourceSize*12 || source.Height<SourceSize*12)
            throw new InvalidDataException("La hoja del Caballero está incompleta.");
        // Destino: las 32 casillas del motor oneko; origen: columna, fila y reflejo.
        int[][] poses={
            new[]{0,0,0,5,0},new[]{0,1,1,5,0},
            new[]{1,0,0,0,1},new[]{1,1,1,0,1},
            new[]{2,0,9,0,0},new[]{2,1,7,8,0},
            new[]{3,0,0,0,0},new[]{3,1,1,0,0},
            new[]{4,0,0,0,1},new[]{4,1,1,0,1},
            new[]{5,0,6,5,0},new[]{6,0,7,5,0},new[]{7,0,8,5,0},
            new[]{5,1,6,1,0},new[]{5,2,7,1,0},
            new[]{6,1,6,1,1},new[]{5,3,7,1,1},
            new[]{7,1,9,7,0},
            new[]{0,2,1,1,0},new[]{0,3,2,1,0},
            new[]{1,2,2,5,0},new[]{1,3,3,5,0},
            new[]{2,2,0,3,0},new[]{2,3,1,3,0},
            new[]{3,2,9,7,0},new[]{3,3,6,5,0},
            new[]{4,2,2,0,1},new[]{4,3,3,0,1},
            new[]{6,2,10,5,0},new[]{6,3,6,5,0},
            new[]{7,2,7,5,0},new[]{7,3,8,5,0}
        };
        var atlas=new Bitmap(CellSize*8,CellSize*4,PixelFormat.Format32bppArgb);
        using(Graphics g=Graphics.FromImage(atlas)) {
            g.CompositingMode=CompositingMode.SourceCopy;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            foreach(int[] p in poses) {
                Rectangle cell=new Rectangle(p[2]*SourceSize,p[3]*SourceSize,SourceSize,SourceSize);
                using(Bitmap tile=source.Clone(cell,PixelFormat.Format32bppArgb)) {
                    if(p[4]!=0)tile.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImage(tile,new Rectangle(p[0]*CellSize+4,p[1]*CellSize+4,CellSize-8,CellSize-8),0,0,SourceSize,SourceSize,GraphicsUnit.Pixel);
                }
            }
        }
        return atlas;
    }
}
