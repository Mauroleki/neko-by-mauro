using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;

internal sealed class PetSettings
{
    public string Name = "Oneko";
    public int Design, Scale = 1;
    public bool Independent, ShowName = true, AutoSleep = true;
    public int Slot, CatCount=1, PetCount;
    public bool Sounds, KeyboardReaction=true, DailyRoutine=true, Entrance=true, EdgePeek=true;
    string FilePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OnekoByMau",Slot==0?"settings.xml":"cat"+(Slot+1)+".xml"); } }
    public static PetSettings Load(int slot=0)
    {
        var s = new PetSettings { Slot=slot, Name=slot==0?"Oneko":"Gato "+(slot+1) };
        try {
            var doc = new XmlDocument { XmlResolver = null }; doc.Load(s.FilePath);
            XmlElement r = doc.DocumentElement;
            s.Name = CleanName(r.GetAttribute("name"));
            int n;
            if (int.TryParse(r.GetAttribute("design"),out n)) s.Design = Math.Max(0,Math.Min(SkinFactory.Names.Length-1,n));
            if (int.TryParse(r.GetAttribute("scale"),out n)) s.Scale = Math.Max(1,Math.Min(3,n));
            s.Independent = r.GetAttribute("independent") == "true";
            s.ShowName = r.GetAttribute("showName") != "false";
            s.AutoSleep = r.GetAttribute("autoSleep") != "false";
            if(int.TryParse(r.GetAttribute("catCount"),out n))s.CatCount=Math.Max(1,Math.Min(4,n));
            if(int.TryParse(r.GetAttribute("petCount"),out n))s.PetCount=Math.Max(0,n);
            s.Sounds=r.GetAttribute("sounds")=="true";
            s.KeyboardReaction=r.GetAttribute("keyboardReaction")!="false";
            s.DailyRoutine=r.GetAttribute("dailyRoutine")!="false";
            s.Entrance=r.GetAttribute("entrance")!="false";
            s.EdgePeek=r.GetAttribute("edgePeek")!="false";
        } catch (IOException) { } catch (UnauthorizedAccessException) { } catch (XmlException) { }
        return s;
    }
    public static string CleanName(string value)
    {
        var result = new StringBuilder();
        foreach (char c in value ?? "") if (!char.IsControl(c) && !char.IsSurrogate(c)) result.Append(c);
        string name = result.ToString().Trim();
        if (name.Length > 20) name = name.Substring(0,20);
        return name.Length == 0 ? "Oneko" : name;
    }
    public bool Save()
    {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            var doc = new XmlDocument(); var r = doc.CreateElement("oneko"); doc.AppendChild(r);
            r.SetAttribute("name",Name); r.SetAttribute("design",Design.ToString()); r.SetAttribute("scale",Scale.ToString());
            r.SetAttribute("independent",Independent ? "true":"false");
            r.SetAttribute("showName",ShowName ? "true":"false"); r.SetAttribute("autoSleep",AutoSleep ? "true":"false");
            r.SetAttribute("catCount",CatCount.ToString());r.SetAttribute("petCount",PetCount.ToString());
            r.SetAttribute("sounds",Sounds?"true":"false");r.SetAttribute("keyboardReaction",KeyboardReaction?"true":"false");
            r.SetAttribute("dailyRoutine",DailyRoutine?"true":"false");r.SetAttribute("entrance",Entrance?"true":"false");
            r.SetAttribute("edgePeek",EdgePeek?"true":"false");
            string temp = FilePath + ".tmp"; doc.Save(temp);
            if (File.Exists(FilePath)) File.Replace(temp,FilePath,null); else File.Move(temp,FilePath);
            return true;
        } catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; }
    }
}

internal static class SkinFactory
{
    public static readonly string[] Names = PetCatalog.Names;
    public static int FrameSize(int design) { return PetCatalog.HasAtlas(design) ? 128 : design>=15 ? PetSprites.CellSize : design==13 ? KarnalitoSprites.CellSize : design==14 ? KnightSprites.CellSize : 32; }
    static readonly Color[] Fur = { Color.White, C(247,173,87), C(163,174,192), C(70,77,91), C(247,225,176), C(246,174,199), C(193,175,239), C(158,211,241) };
    static readonly Color[] Ink = { Color.Black, C(89,46,26), C(42,48,61), C(16,19,27), C(94,67,37), C(102,43,68), C(60,44,94), C(34,66,91) };
    static Color C(int r,int g,int b) { return Color.FromArgb(r,g,b); }
    // Recolour only original opaque pixels. Silhouettes and every animation are preserved.
    public static Bitmap Make(Bitmap original,int design)
    {
        if(PetCatalog.HasAtlas(design))return PetCatalog.Load(design);
        if(design==13)return KarnalitoSprites.Load();
        if(design==14)return KnightSprites.Load();
        if(design>=15)return PetSprites.Load(design);
        var result = new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb);
        for (int cy=0;cy<4;cy++) for (int cx=0;cx<8;cx++) {
            int minX=32,minY=32,maxX=0,maxY=0;
            for(int y=0;y<32;y++) for(int x=0;x<32;x++) if(original.GetPixel(cx*32+x,cy*32+y).A>0) {
                minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);
            }
            for(int y=0;y<32;y++) for(int x=0;x<32;x++) {
                Color old = original.GetPixel(cx*32+x,cy*32+y);
                if(old.A==0) continue;
                bool white = old.R>128;
                Color color;
                if(design<8) color=white ? Fur[design]:Ink[design];
                else {
                    double u=(x-minX)/(double)Math.Max(1,maxX-minX),v=(y-minY)/(double)Math.Max(1,maxY-minY);
                    bool sleeping = cx==2 && cy<2;
                    color=C(31,28,30);
                    if(white) {
                        if(design==8) { // Siamese points: dark ears, mask, paws and tail-side.
                            bool points = v<0.18 || (v<0.48 && Math.Abs(u-0.5)<0.29) || v>0.86;
                            if(sleeping) points = u<0.32 || (u>0.83 && v>0.45);
                            color=points ? C(111,77,60):C(242,221,183);
                        } else if(design==9 || design==10) {
                            bool stripes=((y-minY)%7<2 && (u<0.32 || u>0.68)) || (v<0.3 && (x-minX)%6<2);
                            color=design==9 ? (stripes?C(158,81,37):C(243,164,75)) : (stripes?C(80,91,105):C(179,188,199));
                            if(v>0.87) color=C(248,240,222);
                        } else if(design==11) {
                            bool orange=(u-0.25)*(u-0.25)+(v-0.30)*(v-0.30)<0.075;
                            bool black=(u-0.81)*(u-0.81)+(v-0.66)*(v-0.66)<0.065;
                            color=orange?C(232,152,66):black?C(66,60,59):C(255,249,232);
                        } else {
                            color=TuxedoWhite(cx,cy,x,y)?C(255,250,239):C(48,55,65);
                        }
                    }
                }
                result.SetPixel(cx*32+x,cy*32+y,color);
            }
        }
        return result;
    }
    static bool In(int x,int y,int l,int t,int r,int b) { return x>=l && x<=r && y>=t && y<=b; }
    // Hand-placed markings for each pose: white muzzle, chest and socks.
    // Back-facing poses retain a black back, rather than a moving white stripe.
    static bool TuxedoWhite(int cx,int cy,int x,int y)
    {
        switch(cy*8+cx) {
            case 0: case 8: return In(x,y,3,25,9,28)||In(x,y,22,25,28,28);
            case 1: return In(x,y,3,10,12,13)||In(x,y,11,14,16,19)||In(x,y,0,14,4,17)||In(x,y,21,27,25,31);
            case 9: return In(x,y,5,9,11,13)||In(x,y,8,13,12,18)||In(x,y,13,27,18,30)||In(x,y,25,25,29,28);
            case 2: case 10: return In(x,y,12,24,23,28)||In(x,y,23,26,29,29);
            case 3: return In(x,y,24,13,30,16)||In(x,y,21,17,25,20)||In(x,y,0,23,4,26)||In(x,y,21,23,28,26);
            case 11: return In(x,y,24,22,30,25)||In(x,y,21,23,25,27)||In(x,y,3,22,8,25)||In(x,y,16,28,24,30);
            case 4: return In(x,y,3,11,10,14)||In(x,y,8,15,12,21)||In(x,y,0,17,6,20)||In(x,y,9,26,16,29);
            case 12: return In(x,y,3,10,10,13)||In(x,y,8,14,12,20)||In(x,y,8,27,16,29);
            case 5: return In(x,y,8,15,18,17)||In(x,y,12,18,16,26)||In(x,y,8,28,13,30)||In(x,y,20,28,24,30);
            case 6: return In(x,y,8,15,18,18)||In(x,y,12,18,16,26)||In(x,y,7,28,12,30)||In(x,y,19,28,23,30);
            case 7: return In(x,y,14,11,22,15)||In(x,y,17,16,20,24)||In(x,y,7,28,12,30)||In(x,y,22,27,27,30);
            case 13: return In(x,y,21,24,28,27)||In(x,y,18,25,22,28)||In(x,y,21,29,27,31)||In(x,y,2,8,6,11);
            case 14: return In(x,y,4,25,12,28)||In(x,y,12,22,16,26)||In(x,y,4,29,9,31)||In(x,y,26,9,30,13);
            case 15: return In(x,y,12,21,22,23)||In(x,y,12,10,22,12)||In(x,y,12,27,15,31)||In(x,y,20,27,23,31);
            case 16: return In(x,y,20,10,28,13)||In(x,y,17,14,22,19)||In(x,y,28,14,31,17)||In(x,y,6,27,10,31);
            case 17: return In(x,y,11,27,16,30)||In(x,y,20,27,24,30);
            case 18: return In(x,y,23,15,30,18)||In(x,y,20,18,25,23)||In(x,y,25,21,29,23)||In(x,y,19,28,25,30);
            case 19: case 27: case 31:
                return In(x,y,11,13,21,16)|| (y>=17 && y<=25 && Math.Abs(x-16)<=Math.Max(2,5-(y-17)/2)) || In(x,y,6,25,11,27)||In(x,y,21,25,25,27);
            case 20: return In(x,y,1,13,7,16)||In(x,y,6,17,10,20)||In(x,y,27,23,31,26)||In(x,y,3,23,10,26);
            case 21: return In(x,y,20,25,28,28)||In(x,y,15,22,19,26)||In(x,y,23,29,28,31)||In(x,y,1,9,5,13);
            case 22: return In(x,y,12,21,22,23)||In(x,y,12,9,22,11)||In(x,y,12,27,15,31)||In(x,y,20,27,23,31);
            case 23: case 30: return In(x,y,12,26,22,28)||In(x,y,12,14,22,16)||In(x,y,7,25,10,28)||In(x,y,24,25,27,28);
            case 24: return In(x,y,20,9,26,13)||In(x,y,20,13,24,18)||In(x,y,15,27,20,30)||In(x,y,3,25,7,28);
            case 25: return In(x,y,11,27,16,30)||In(x,y,20,27,24,30);
            case 26: return In(x,y,23,12,30,15)||In(x,y,20,16,24,22)||In(x,y,19,27,25,29);
            case 28: return In(x,y,1,22,7,25)||In(x,y,6,23,10,27)||In(x,y,23,22,28,25)||In(x,y,7,28,15,30);
            case 29: return In(x,y,3,24,10,27)||In(x,y,9,25,13,28)||In(x,y,4,29,10,31)||In(x,y,25,8,29,11);
        }
        return false;
    }
}

internal class SpriteOverlay : Form
{
    public SpriteOverlay()
    {
        AutoScaleMode=AutoScaleMode.None; FormBorderStyle=FormBorderStyle.None;
        ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;
        ClientSize=new Size(1,1);
    }
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams { get { var p=base.CreateParams; p.ExStyle|=0x80000|0x20|0x80|0x08000000;return p; } }
    protected override void WndProc(ref Message m)
    {
        if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
        base.WndProc(ref m);
    }
    public void SetInteractive(bool enabled)
    {
        int style=Native.GetWindowLong(Handle,-20);
        Native.SetWindowLong(Handle,-20,enabled ? style & ~0x20 : style | 0x20);
    }
    public void Present(Bitmap bitmap,int x,int y)
    {
        IntPtr dc=Native.GetDC(IntPtr.Zero),memory=Native.CreateCompatibleDC(dc);
        IntPtr hb=bitmap.GetHbitmap(Color.FromArgb(0)),previous=Native.SelectObject(memory,hb);
        var point=new Native.POINT(x,y);var origin=new Native.POINT(0,0);var size=new Native.SIZE(bitmap.Width,bitmap.Height);
        var blend=new Native.BLENDFUNCTION { SourceConstantAlpha=255,AlphaFormat=1 };
        try {
            if(!Native.UpdateLayeredWindow(Handle,dc,ref point,ref size,memory,ref origin,0,ref blend,2))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            Native.SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x1|0x2|0x10);
        } finally {
            Native.SelectObject(memory,previous);Native.DeleteObject(hb);Native.DeleteDC(memory);Native.ReleaseDC(IntPtr.Zero,dc);
        }
    }
}

internal sealed class ToyWindow : SpriteOverlay
{
    public double X,Y,VX,VY;
    public bool Dragging;
    readonly int kind;
    Point lastMouse;
    public ToyWindow(int type,Point position)
    {
        kind=type;X=position.X;Y=position.Y;
        ClientSize=new Size(28,28);
        Location=new Point(position.X-14,position.Y-14);
        Shown+=delegate{SetInteractive(true);DrawToy();};
        MouseDown+=delegate(object sender,MouseEventArgs e){
            if(e.Button!=MouseButtons.Left)return;
            Dragging=true;Capture=true;VX=VY=0;lastMouse=Cursor.Position;
        };
        MouseMove+=delegate{
            if(!Dragging)return;
            Point p=Cursor.Position;VX=Math.Max(-18,Math.Min(18,p.X-lastMouse.X));VY=Math.Max(-18,Math.Min(18,p.Y-lastMouse.Y));
            X=p.X;Y=p.Y;lastMouse=p;DrawToy();
        };
        MouseUp+=delegate{Dragging=false;Capture=false;};
        MouseCaptureChanged+=delegate{if(!Capture)Dragging=false;};
    }
    public void Step()
    {
        if(kind==4){Point p=Cursor.Position;X=p.X;Y=p.Y;DrawToy();return;}
        if(!Dragging) {
            X+=VX;Y+=VY;VX*=0.965;VY*=0.965;
            Rectangle r=Screen.FromPoint(new Point((int)X,(int)Y)).WorkingArea;
            if(X<r.Left+14){X=r.Left+14;VX=Math.Abs(VX)*0.8;}
            if(X>r.Right-14){X=r.Right-14;VX=-Math.Abs(VX)*0.8;}
            if(Y<r.Top+14){Y=r.Top+14;VY=Math.Abs(VY)*0.8;}
            if(Y>r.Bottom-14){Y=r.Bottom-14;VY=-Math.Abs(VY)*0.8;}
        }
        DrawToy();
    }
    void DrawToy()
    {
        using(var b=new Bitmap(28,28,PixelFormat.Format32bppPArgb))using(Graphics g=Graphics.FromImage(b)){
            g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var fill=new SolidBrush(kind==1?Color.FromArgb(173,127,219):kind==2?Color.FromArgb(195,220,146):kind==3?Color.FromArgb(236,113,161):kind==4?Color.Red:Color.FromArgb(243,162,63)))
            using(var outline=new Pen(kind==1?Color.FromArgb(91,58,130):Color.FromArgb(123,68,28),2)){
                g.FillEllipse(fill,3,3,21,21);g.DrawEllipse(outline,3,3,21,21);
                if(kind==1){g.DrawArc(outline,6,4,10,19,70,240);g.DrawArc(outline,11,4,10,19,100,230);g.DrawArc(outline,3,8,21,10,0,180);}
                else if(kind==2){g.FillEllipse(Brushes.Pink,17,8,7,5);g.DrawArc(outline,1,10,13,11,45,220);}
                else if(kind==3){g.DrawLine(outline,4,24,15,14);g.DrawLine(outline,15,14,22,5);}
                else if(kind==4){g.FillEllipse(Brushes.Red,9,9,10,10);}
                else {g.DrawLine(outline,7,7,21,21);g.DrawArc(outline,4,5,17,15,10,165);}
                g.FillEllipse(Brushes.White,7,6,4,3);
            }
            Present(b,(int)X-14,(int)Y-14);
        }
    }
}

internal static class WindowPerch
{
    public static bool Bounds(IntPtr hwnd,out Rectangle bounds)
    {
        bounds=Rectangle.Empty;
        if(hwnd==IntPtr.Zero || !Native.IsWindow(hwnd) || !Native.IsWindowVisible(hwnd) || Native.IsIconic(hwnd))return false;
        int cloaked;
        if(Native.DwmGetWindowAttribute(hwnd,14,out cloaked,4)==0 && cloaked!=0)return false;
        Native.RECT r;
        if(Native.DwmGetWindowAttributeRect(hwnd,9,out r,16)!=0 && !Native.GetWindowRect(hwnd,out r))return false;
        bounds=Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom);return bounds.Width>150 && bounds.Height>90;
    }
    public static IntPtr Find(Point near,int catSize)
    {
        IntPtr chosen=IntPtr.Zero;Rectangle area=Screen.FromPoint(near).WorkingArea;
        Native.EnumWindows(delegate(IntPtr hwnd,IntPtr unused){
            uint pid;Native.GetWindowThreadProcessId(hwnd,out pid);
            if(pid==Native.GetCurrentProcessId() || (Native.GetWindowLong(hwnd,-20)&0x80)!=0)return true;
            var name=new StringBuilder(128);Native.GetClassName(hwnd,name,128);
            if(name.ToString()=="Progman" || name.ToString()=="WorkerW" || name.ToString()=="Shell_TrayWnd")return true;
            Rectangle r;
            if(!Bounds(hwnd,out r) || r.Top<area.Top+catSize+12 || r.Top>area.Bottom-40 || r.Right<area.Left+catSize || r.Left>area.Right-catSize)return true;
            chosen=hwnd;return false;
        },IntPtr.Zero);
        return chosen;
    }
}

internal static class NamePrompt
{
    public static string Ask(string current)
    {
        using(var dialog=new Form())using(var input=new TextBox())using(var ok=new Button())using(var cancel=new Button())using(var label=new Label()){
            dialog.Text="Nombre de tu gato";dialog.ClientSize=new Size(330,140);dialog.FormBorderStyle=FormBorderStyle.FixedDialog;
            dialog.MaximizeBox=false;dialog.MinimizeBox=false;dialog.StartPosition=FormStartPosition.CenterScreen;dialog.TopMost=true;
            label.Text="¿Cómo se llama? (máximo 20 caracteres)";label.SetBounds(16,16,300,24);
            input.Text=current;input.MaxLength=20;input.SetBounds(16,47,296,26);
            ok.Text="Guardar";ok.DialogResult=DialogResult.OK;ok.SetBounds(128,92,88,30);
            cancel.Text="Cancelar";cancel.DialogResult=DialogResult.Cancel;cancel.SetBounds(224,92,88,30);
            dialog.Controls.AddRange(new Control[]{label,input,ok,cancel});dialog.AcceptButton=ok;dialog.CancelButton=cancel;
            dialog.Shown+=delegate{input.Focus();input.SelectAll();};
            return dialog.ShowDialog()==DialogResult.OK ? PetSettings.CleanName(input.Text):null;
        }
    }
}
