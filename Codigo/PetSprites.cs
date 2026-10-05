using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

// Adapts the supplied sheets to the animation cells used by the desktop pet.
internal static class PetSprites
{
    public const int CellSize=128;
    static Rectangle R(int x,int y,int w,int h){return new Rectangle(x,y,w,h);}
    public static Bitmap Load(int design)
    {
        string name=design==15?"hornet.png":design==16?"charmander.png":"kirby.png";
        using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)){
            if(stream==null)throw new FileNotFoundException("Faltan los sprites de "+name);
            using(var source=new Bitmap(stream)){
                if(design!=16)return Build(source,design,null);
                using(Stream rearStream=Assembly.GetExecutingAssembly().GetManifestResourceStream("charmander-back.png")){
                    if(rearStream==null)throw new FileNotFoundException("Falta la pose trasera de Charmander.");
                    using(var rear=new Bitmap(rearStream))return Build(source,design,rear);
                }
            }
        }
    }
    public static Bitmap Build(Bitmap source,int design,Bitmap rear)
    {
        Rectangle[] rects;
        int[] east,north,south,sleep;
        int idle,pet;
        if(design==15){
            rects=new[]{R(31,42,155,195),R(189,39,157,194),R(363,32,160,194),R(520,36,160,196),R(681,37,156,195),R(33,241,177,217),R(197,245,177,219),R(373,240,179,218)};
            east=new[]{0,2};north=east;south=east;sleep=new[]{5,7};idle=5;pet=6;
        }else if(design==16){
            rects=new Rectangle[16];
            for(int i=0;i<16;i++)rects[i]=(i>=4&&i<8)?R(22,28,44,45):R(28,27,42,46);
            east=new[]{0,2};north=new[]{4,6};south=new[]{8,10};sleep=new[]{14,15};idle=12;pet=13;
        }else{
            rects=new[]{R(4,13,31,23),R(36,14,32,22),R(75,18,36,18),R(115,22,37,14),R(4,79,34,28),R(44,85,29,27),R(80,88,26,24),R(113,79,34,27),R(7,235,25,29),R(33,236,29,28),R(64,236,27,28),R(93,235,25,30),R(113,203,40,22),R(158,207,42,18),R(80,122,25,33),R(111,123,28,31)};
            east=new[]{4,5};north=new[]{8,10};south=new[]{0,1};sleep=new[]{12,13};idle=0;pet=14;
        }
        var frames=new List<Bitmap>();
        try{
            for(int index=0;index<rects.Length;index++){
                Rectangle rect=rects[index];Bitmap input=design==16&&index>=4&&index<8?rear:source;
                if(input==null || rect.X<0 || rect.Y<0 || rect.Right>input.Width || rect.Bottom>input.Height)throw new InvalidDataException("Hoja de sprites incompleta.");
                var frame=new Bitmap(rect.Width,rect.Height,PixelFormat.Format32bppArgb);
                for(int fy=0;fy<rect.Height;fy++)for(int fx=0;fx<rect.Width;fx++)frame.SetPixel(fx,fy,input.GetPixel(rect.X+fx,rect.Y+fy));
                if(design==16 && !(index>=4&&index<8))frame.RotateFlip(RotateFlipType.RotateNoneFlipX);
                if(design!=16)ClearBackdrop(frame,design==15?128:255);
                if(design==15)KeepMainFigure(frame);
                frames.Add(frame);
            }
            var atlas=new Bitmap(CellSize*8,CellSize*4,PixelFormat.Format32bppArgb);
            using(Graphics g=Graphics.FromImage(atlas)){
                g.InterpolationMode=design==15?InterpolationMode.HighQualityBicubic:InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode=PixelOffsetMode.Half;
                for(int y=0;y<4;y++)for(int x=0;x<8;x++)Draw(g,frames[idle],x,y,false,design);
                Pair(g,frames,east,3,0,3,1,false,design);
                Pair(g,frames,east,4,2,4,3,true,design);
                Pair(g,frames,north,1,2,1,3,false,design);
                Pair(g,frames,south,6,3,7,2,false,design);
                Pair(g,frames,east,0,2,0,3,false,design);
                Pair(g,frames,east,5,1,5,2,false,design);
                Pair(g,frames,east,1,0,1,1,true,design);
                Pair(g,frames,east,5,3,6,1,true,design);
                Pair(g,frames,sleep,2,0,2,1,false,design);
                Draw(g,frames[pet],7,3,false,design);
                Draw(g,frames[sleep[0]],3,2,false,design);
                foreach(int x in new[]{5,6,7})Draw(g,frames[pet],x,0,false,design);
                Pair(g,frames,new[]{idle,pet},0,0,0,1,false,design);
                Pair(g,frames,new[]{idle,pet},7,1,6,2,false,design);
                Pair(g,frames,new[]{idle,pet},2,2,2,3,false,design);
                Pair(g,frames,new[]{idle,pet},4,0,4,1,true,design);
            }
            return atlas;
        }finally{foreach(Bitmap frame in frames)frame.Dispose();}
    }
    static void Pair(Graphics g,List<Bitmap> frames,int[] pair,int x1,int y1,int x2,int y2,bool flip,int design){Draw(g,frames[pair[0]],x1,y1,flip,design);Draw(g,frames[pair[1]],x2,y2,flip,design);}
    static void Draw(Graphics g,Bitmap frame,int x,int y,bool flip,int design)
    {
        var cell=R(x*CellSize,y*CellSize,CellSize,CellSize);
        g.CompositingMode=CompositingMode.SourceCopy;
        using(var clear=new SolidBrush(Color.Transparent))g.FillRectangle(clear,cell);
        float scale=design==15?0.49f:design==17?2.65f:2.3f;
        int w=(int)Math.Round(frame.Width*scale),h=(int)Math.Round(frame.Height*scale);
        int bounce=design==16&&((x==3&&y==1)||(x==4&&y==3)||(x==1&&y==3)||(x==7&&y==2)||(x==0&&y==3)||(x==5&&y==2)||(x==1&&y==1)||(x==6&&y==1))?5:0;
        var state=g.Save();
        if(flip){g.TranslateTransform(cell.X*2+CellSize,0);g.ScaleTransform(-1,1);}
        g.DrawImage(frame,R(cell.X+(CellSize-w)/2,cell.Y+CellSize-h-6-bounce,w,h),0,0,frame.Width,frame.Height,GraphicsUnit.Pixel);
        g.Restore(state);
    }

    static void ClearBackdrop(Bitmap frame,int backdrop)
    {
        // Flood only the outside, preserving enclosed white eyes and faces.
        int w=frame.Width,h=frame.Height;var seen=new bool[w*h];var queue=new Queue<Point>();
        for(int x=0;x<w;x++){queue.Enqueue(new Point(x,0));queue.Enqueue(new Point(x,h-1));}
        for(int y=0;y<h;y++){queue.Enqueue(new Point(0,y));queue.Enqueue(new Point(w-1,y));}
        while(queue.Count>0){Point p=queue.Dequeue();if(p.X<0||p.Y<0||p.X>=w||p.Y>=h)continue;int i=p.Y*w+p.X;if(seen[i])continue;seen[i]=true;
            Color c=frame.GetPixel(p.X,p.Y);
            if(Math.Abs(c.R-backdrop)>16||Math.Abs(c.G-backdrop)>16||Math.Abs(c.B-backdrop)>16)continue;
            frame.SetPixel(p.X,p.Y,Color.Transparent);
            queue.Enqueue(new Point(p.X-1,p.Y));queue.Enqueue(new Point(p.X+1,p.Y));queue.Enqueue(new Point(p.X,p.Y-1));queue.Enqueue(new Point(p.X,p.Y+1));
        }
    }
    static void KeepMainFigure(Bitmap frame)
    {
        int w=frame.Width,h=frame.Height;var visited=new bool[w*h];var largest=new List<Point>();
        for(int y=0;y<h;y++)for(int x=0;x<w;x++){
            if(visited[y*w+x]||frame.GetPixel(x,y).A==0)continue;
            var group=new List<Point>();var pending=new Queue<Point>();pending.Enqueue(new Point(x,y));
            while(pending.Count>0){Point p=pending.Dequeue();if(p.X<0||p.Y<0||p.X>=w||p.Y>=h||visited[p.Y*w+p.X])continue;visited[p.Y*w+p.X]=true;if(frame.GetPixel(p.X,p.Y).A==0)continue;group.Add(p);
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(dx!=0||dy!=0)pending.Enqueue(new Point(p.X+dx,p.Y+dy));
            }
            if(group.Count>largest.Count)largest=group;
        }
        var keep=new bool[w*h];foreach(Point p in largest)keep[p.Y*w+p.X]=true;
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(!keep[y*w+x])frame.SetPixel(x,y,Color.Transparent);
    }
}
