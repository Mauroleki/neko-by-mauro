// NekoCat By Mauro. Desktop adaptation of oneko.js (adryd, MIT).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("NekoCat By Mauro")]
[assembly: AssemblyProduct("NekoCat By Mauro")]
[assembly: AssemblyDescription("Mascotas animadas para el escritorio")]
[assembly: AssemblyVersion("2.0.1.0")]
[assembly: AssemblyFileVersion("2.0.1.0")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7.2", FrameworkDisplayName=".NET Framework 4.7.2")]

internal static class Program
{
    [STAThread] static void Main()
    {
        bool created;
        using(var mutex=new Mutex(true,"Local\\OnekoEscritorioCriss2026",out created)) {
            if(!created)return;
            try {
                try{Native.SetProcessDpiAwarenessContext(new IntPtr(-4));}catch(EntryPointNotFoundException){Native.SetProcessDPIAware();}
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new CatForm());
            }catch(Exception ex){MessageBox.Show("No se pudo abrir NekoCat.\n\n"+ex.Message,"NekoCat By Mauro",MessageBoxButtons.OK,MessageBoxIcon.Error);}
            finally{mutex.ReleaseMutex();}
        }
    }
}

internal sealed class CatForm : SpriteOverlay
{
    readonly PetSettings settings;
    readonly CatForm leader;
    readonly int slot;
    readonly List<CatForm> companions=new List<CatForm>();
    readonly Dictionary<int,PetPlace> places=new Dictionary<int,PetPlace>();
    readonly Random random=new Random();
    readonly NekoEngine neko;
    readonly Bitmap original;
    Bitmap sheet;
    readonly Dictionary<string,Point[]> sprites=new Dictionary<string,Point[]>();
    readonly Dictionary<string,Bitmap> frames=new Dictionary<string,Bitmap>();
    readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
    readonly NotifyIcon tray=new NotifyIcon();
    readonly ContextMenuStrip menu=new ContextMenuStrip();
    readonly ToolStripMenuItem pause=new ToolStripMenuItem("Pausar");
    readonly ToolStripMenuItem independent=new ToolStripMenuItem("Paseo independiente");
    readonly ToolStripMenuItem petMode=new ToolStripMenuItem("Modo caricias (clic en el gato)");
    readonly ToolStripMenuItem showName=new ToolStripMenuItem("Mostrar nombre");
    readonly ToolStripMenuItem autoSleep=new ToolStripMenuItem("Dormir sobre ventanas automáticamente");
    readonly ToolStripMenuItem startWithWindows=new ToolStripMenuItem("Iniciar con Windows");
    readonly List<ToolStripMenuItem> designs=new List<ToolStripMenuItem>(),sizes=new List<ToolStripMenuItem>();
    readonly Icon catIcon;
    readonly SpriteOverlay badge=new SpriteOverlay();
    ToyWindow toy;
    IntPtr perch=IntPtr.Zero;
    double perchFraction=0.5;
    bool explicitSleep,paused,cleaned,renaming;
    int ticks,idleTicks,sleepTicks,heartTicks,petTicks,wanderWait,kickCooldown,stretchTicks,boxTicks,placeCooldown,giftTicks,edgeTicks,lastPetCountTick=-100,sleepAtKind=-1,lastTypingTick=-300;
    PetPlace destination;
    string gift="";
    Point lastMouse,sleepMouse,wanderGoal;
    bool hasGoal;
    bool settingsWarning;
    string startupLoadError;
    string startupRegistrationError;

    public CatForm():this(0,null){}
    CatForm(int catSlot,CatForm parent)
    {
        slot=catSlot;leader=parent??this;settings=PetSettings.Load(slot);
        Text="NekoCat By Mauro";
        using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("oneko.gif"))
        using(var image=new Bitmap(stream))original=new Bitmap(image);
        try { sheet=SkinFactory.Make(original,settings.Design); }
        catch(Exception ex) {
            startupLoadError=ex.Message;settings.Design=0;
            sheet=SkinFactory.Make(original,0);
        }
        Add("idle",3,3);Add("alert",7,3);Add("scratchSelf",5,0,6,0,7,0);
        Add("scratchWallN",0,0,0,1);Add("scratchWallS",7,1,6,2);Add("scratchWallE",2,2,2,3);Add("scratchWallW",4,0,4,1);
        Add("tired",3,2);Add("sleeping",2,0,2,1);Add("N",1,2,1,3);Add("NE",0,2,0,3);Add("E",3,0,3,1);
        Add("SE",5,1,5,2);Add("S",6,3,7,2);Add("SW",5,3,6,1);Add("W",4,2,4,3);Add("NW",1,0,1,1);
        lastMouse=Cursor.Position;
        neko=new NekoEngine(lastMouse.X-80-slot*42,lastMouse.Y+50+slot*30,Environment.TickCount+slot*1009);
        ConfigureSize();ClampTo(Screen.FromPoint(lastMouse).WorkingArea);
        ClientSize=new Size(Side,Side);Location=new Point((int)neko.X-Side/2,(int)neko.Y-Side/2);
        using(Bitmap image=original.Clone(new Rectangle(96,96,32,32),PixelFormat.Format32bppArgb)) {
            IntPtr h=image.GetHicon();using(Icon temporary=Icon.FromHandle(h))catIcon=(Icon)temporary.Clone();Native.DestroyIcon(h);
        }
        BuildMenu();
        tray.Icon=catIcon;tray.ContextMenuStrip=menu;UpdateTrayText();tray.Visible=true;
        tray.DoubleClick+=delegate{TogglePause();};
        MouseDown+=delegate(object sender,MouseEventArgs e){if(petMode.Checked && e.Button==MouseButtons.Left)Pet();};
        MouseMove+=delegate(object sender,MouseEventArgs e){if(petMode.Checked && e.Button==MouseButtons.Left)Pet();};
        timer.Interval=100;timer.Tick+=delegate{TickCat();};
        Shown+=delegate{
            if(settings.Entrance){Rectangle a=Screen.FromPoint(CatPoint).WorkingArea;neko.X=a.Left+Side/2;neko.Y=Math.Max(a.Top+Side/2,Math.Min(a.Bottom-Side/2,Cursor.Position.Y+slot*25));}
            DrawCat();timer.Start();
            if(slot==0){
                LoadPlaces();
                for(int i=1;i<settings.CatCount;i++)AddCat(i,false);
                new WelcomeToast(catIcon).Show(this);
                UpdateManager.StartWatching(this);
            }
            if(startupLoadError!=null)Notice("No se pudo cargar el personaje guardado. Abrí el gato clásico. Detalle: "+startupLoadError);
            if(startupRegistrationError!=null)Notice("No pude actualizar el inicio automático. Puedes intentarlo desde el menú. Detalle: "+startupRegistrationError);
        };
    }
    int Side {get{return 32*settings.Scale;}}
    Point CatPoint {get{return new Point((int)neko.X,(int)neko.Y);}}
    void Add(string name,params int[] cells){var points=new Point[cells.Length/2];for(int i=0;i<points.Length;i++)points[i]=new Point(cells[2*i],cells[2*i+1]);sprites[name]=points;}
    void Save(){if(!settings.Save() && !settingsWarning){settingsWarning=true;Notice("No pude guardar los ajustes. Se mantendrán durante esta sesión.");}}
    void Notice(string text){tray.ShowBalloonTip(4000,"NekoCat By Mauro",text,ToolTipIcon.Info);}
    static void SortPetMenu(ToolStripMenuItem parent){var items=new List<ToolStripItem>();foreach(ToolStripItem item in parent.DropDownItems)items.Add(item);items.Sort(delegate(ToolStripItem a,ToolStripItem b){return string.Compare(a.Text,b.Text,StringComparison.CurrentCultureIgnoreCase);});parent.DropDownItems.Clear();parent.DropDownItems.AddRange(items.ToArray());}
    void BuildMenu()
    {
        menu.Items.Add(new ToolStripMenuItem("NekoCat By Mauro · 2.0.1 · "+settings.Name) {Enabled=false});
        menu.Items.Add("Ponerle nombre…",null,delegate{
            renaming=true;string name;try{name=NamePrompt.Ask(settings.Name);}finally{renaming=false;}
            if(name!=null){settings.Name=name;menu.Items[0].Text="NekoCat By Mauro · 2.0.1 · "+name;UpdateTrayText();Save();DrawBadge();}
        });
        showName.Checked=settings.ShowName;showName.CheckOnClick=true;
        showName.CheckedChanged+=delegate{settings.ShowName=showName.Checked;Save();DrawBadge();};menu.Items.Add(showName);
        menu.Items.Add(new ToolStripSeparator());
        independent.Checked=settings.Independent;independent.CheckOnClick=true;
        independent.CheckedChanged+=delegate{settings.Independent=independent.Checked;Wake();hasGoal=false;wanderWait=0;Save();};menu.Items.Add(independent);
        petMode.CheckOnClick=true;petMode.CheckedChanged+=delegate{
            SetInteractive(petMode.Checked);Wake();petTicks=0;
            if(petMode.Checked)Notice("Acerca el cursor y haz clic o arrastra suavemente sobre el gato. Desactiva el modo para que deje pasar todos los clics.");
        };menu.Items.Add(petMode);
        menu.Items.Add("Acariciar ahora ♥",null,delegate{Pet();DrawCat();});
        autoSleep.Checked=settings.AutoSleep;autoSleep.CheckOnClick=true;
        autoSleep.CheckedChanged+=delegate{settings.AutoSleep=autoSleep.Checked;if(!autoSleep.Checked)Wake();Save();};menu.Items.Add(autoSleep);
        menu.Items.Add("Dormir sobre una ventana ahora",null,delegate{if(!StartSleep(true))Notice("Deja una ventana sin maximizar y algo de espacio encima de su borde superior. Luego vuelve a intentarlo.");});
        menu.Items.Add("Despertar",null,delegate{Wake();DrawCat();});
        var toys=new ToolStripMenuItem("Juguetes");
        toys.DropDownItems.Add("Sacar pelota",null,delegate{leader.CreateToy(0);});
        toys.DropDownItems.Add("Sacar ovillo",null,delegate{leader.CreateToy(1);});
        toys.DropDownItems.Add("Sacar ratoncito",null,delegate{leader.CreateToy(2);});
        toys.DropDownItems.Add("Sacar pluma",null,delegate{leader.CreateToy(3);});
        toys.DropDownItems.Add("Puntero láser (sigue el cursor)",null,delegate{leader.CreateToy(4);});
        toys.DropDownItems.Add("Guardar juguete",null,delegate{leader.RemoveToy();Wake();});menu.Items.Add(toys);
        var accessory=new ToolStripMenuItem("Accesorio");
        string[] accessories={"Ninguno","Moño","Gorrito","Corona","Lentes"};
        for(int i=0;i<accessories.Length;i++){
            int chosen=i;var item=new ToolStripMenuItem(accessories[i]){Checked=i==settings.Accessory};
            item.Click+=delegate{settings.Accessory=chosen;foreach(ToolStripMenuItem other in accessory.DropDownItems)other.Checked=other==item;ClearFrames();Save();DrawCat();};
            accessory.DropDownItems.Add(item);
        }menu.Items.Add(accessory);
        menu.Items.Add("Caricias recibidas: "+settings.PetCount,null,delegate{Notice(settings.Name+" recibió "+settings.PetCount+" caricias ♥");});
        var behavior=new ToolStripMenuItem("Comportamiento y sonidos");
        AddOption(behavior,"Sonidos suaves",settings.Sounds,delegate(bool v){settings.Sounds=v;});
        AddOption(behavior,"Reaccionar al teclado",settings.KeyboardReaction,delegate(bool v){settings.KeyboardReaction=v;});
        AddOption(behavior,"Rutina según la hora",settings.DailyRoutine,delegate(bool v){settings.DailyRoutine=v;});
        AddOption(behavior,"Entrada caminando al iniciar",settings.Entrance,delegate(bool v){settings.Entrance=v;});
        AddOption(behavior,"Asomarse en los bordes",settings.EdgePeek,delegate(bool v){settings.EdgePeek=v;});
        AddOption(behavior,"Sorpresas aleatorias",settings.Surprises,delegate(bool v){settings.Surprises=v;});
        menu.Items.Add(behavior);
        if(slot==0){
            var cats=new ToolStripMenuItem("Varias mascotas (máximo 4)");
            cats.DropDownItems.Add("Agregar mascota",null,delegate{AddCat(settings.CatCount,true);});
            cats.DropDownItems.Add("Quitar última mascota",null,delegate{RemoveLastCat();});
            cats.DropDownItems.Add("Cada mascota tiene su propio icono, nombre, diseño y tamaño") .Enabled=false;
            menu.Items.Add(cats);
            var objects=new ToolStripMenuItem("Objetos del escritorio");
            for(int i=0;i<PetPlace.Names.Length;i++){
                int kind=i;objects.DropDownItems.Add("Poner o mover: "+PetPlace.Names[i],null,delegate{Place(kind,Cursor.Position);});
            }
            objects.DropDownItems.Add("Guardar todos los objetos",null,delegate{ClearPlaces();});
            menu.Items.Add(objects);
        }
        var skins=new ToolStripMenuItem("Mascotas");
        var games=new ToolStripMenuItem("Videojuegos");
        var anime=new ToolStripMenuItem("Anime");
        var others=new ToolStripMenuItem("Otros");
        var valorant=new ToolStripMenuItem("Valorant");
        var catsFolder=new ToolStripMenuItem("Gatos");
        others.DropDownItems.Add(catsFolder);
        skins.DropDownItems.AddRange(new ToolStripItem[]{games,anime,others,valorant});
        for(int i=0;i<SkinFactory.Names.Length;i++){
            int chosen=i;var item=new ToolStripMenuItem(SkinFactory.Names[i]){Checked=i==settings.Design};
            item.Click+=delegate{ChangeDesign(chosen);};designs.Add(item);
            (i<13?catsFolder:PetCatalog.Categories[i]=="Valorant"?valorant:PetCatalog.Categories[i]=="Anime"?anime:PetCatalog.Categories[i]=="Otros"?others:games).DropDownItems.Add(item);
        }
        foreach(ToolStripMenuItem category in new[]{games,anime,others,valorant,catsFolder})SortPetMenu(category);
        menu.Items.Add(skins);
        var sizeMenu=new ToolStripMenuItem("Tamaño");
        for(int i=1;i<=3;i++){
            int chosen=i;var item=new ToolStripMenuItem((32*i)+" px"){Checked=i==settings.Scale};
            item.Click+=delegate{settings.Scale=chosen;ConfigureSize();ClearFrames();Wake();for(int j=0;j<sizes.Count;j++)sizes[j].Checked=j+1==chosen;Save();ClampTo(Screen.FromPoint(CatPoint).WorkingArea);DrawCat();};
            sizes.Add(item);sizeMenu.DropDownItems.Add(item);
        }menu.Items.Add(sizeMenu);
        pause.Click+=delegate{TogglePause();};menu.Items.Add(pause);
        menu.Items.Add("Traer junto al cursor",null,delegate{Wake();neko.X=Cursor.Position.X-70;neko.Y=Cursor.Position.Y+40;ClampTo(Screen.FromPoint(Cursor.Position).WorkingArea);DrawCat();});
        menu.Items.Add(new ToolStripSeparator());
        try { startWithWindows.Checked=StartupManager.RefreshEnabledPath(); }
        catch(Exception ex) { startupRegistrationError=ex.Message; }
        startWithWindows.Click+=delegate {
            try {
                bool enable=!StartupManager.IsEnabled();
                StartupManager.SetEnabled(enable);
                startWithWindows.Checked=StartupManager.IsEnabled();
                Notice(enable ? "Inicio automático activado. NekoCat se abrirá cuando inicies sesión en Windows. Conserva el programa en esta carpeta." : "Inicio automático desactivado. NekoCat solo se abrirá cuando lo ejecutes.");
            } catch(Exception ex) {
                MessageBox.Show("No se pudo cambiar el inicio automático.\n\n"+ex.Message,"NekoCat By Mauro",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
        };
        menu.Opening+=delegate {
            try { startWithWindows.Checked=StartupManager.IsEnabled(); }
            catch { /* Keep the last known state; clicking reports access errors. */ }
        };
        menu.Items.Add(startWithWindows);
        if(slot==0)menu.Items.Add("Buscar actualizaciones",null,async delegate { await UpdateManager.CheckAsync(this,false); });
        menu.Items.Add(slot==0?"Salir":"Quitar última mascota",null,delegate{if(slot==0)Close();else leader.RemoveLastCat();});
    }
    void AddOption(ToolStripMenuItem menuItem,string label,bool value,Action<bool> change)
    {
        var option=new ToolStripMenuItem(label){Checked=value,CheckOnClick=true};
        option.CheckedChanged+=delegate{change(option.Checked);Save();};menuItem.DropDownItems.Add(option);
    }
    void ConfigureSize(){neko.Speed=10*settings.Scale;neko.StopDistance=48*settings.Scale;neko.Margin=Side/2;}
    void ClampTo(Rectangle r){neko.Clamp(r.Left,r.Top,r.Right,r.Bottom);}
    void UpdateTrayText(){tray.Text="NekoCat By Mauro · "+settings.Name+(paused?" (en pausa)":"");}
    void TogglePause(){paused=!paused;pause.Text=paused?"Reanudar":"Pausar";UpdateTrayText();}
    void ClearFrames(){foreach(Bitmap b in frames.Values)b.Dispose();frames.Clear();}
    void ChangeDesign(int value)
    {
        Bitmap replacement;
        try { replacement=SkinFactory.Make(original,value); }
        catch(Exception ex) {
            MessageBox.Show("No se pudo cargar este personaje. Se conserva el anterior.\n\n"+ex.Message,"NekoCat By Mauro",MessageBoxButtons.OK,MessageBoxIcon.Information);
            return;
        }
        Bitmap previous=sheet;int previousDesign=settings.Design;
        ClearFrames();sheet=replacement;settings.Design=value;
        try { DrawCat(); }
        catch(Exception ex) {
            ClearFrames();sheet=previous;settings.Design=previousDesign;replacement.Dispose();
            MessageBox.Show("No se pudo mostrar este personaje. Se conserva el anterior.\n\n"+ex.Message,"NekoCat By Mauro",MessageBoxButtons.OK,MessageBoxIcon.Information);
            return;
        }
        previous.Dispose();
        for(int i=0;i<designs.Count;i++)designs[i].Checked=i==value;
        Save();
    }
    void Pet(){Wake();heartTicks=25;petTicks=20;neko.Sprite="scratchSelf";neko.SpriteFrame=ticks/2;if(ticks-lastPetCountTick>=10 || ticks<lastPetCountTick){lastPetCountTick=ticks;settings.PetCount++;Save();if(settings.Sounds)PetAudio.Play(true);}}
    void Wake(){bool wasSleeping=perch!=IntPtr.Zero || boxTicks>0;perch=IntPtr.Zero;explicitSleep=false;sleepTicks=0;boxTicks=0;idleTicks=0;neko.ResetIdle();if(wasSleeping)stretchTicks=10;}
    void RemoveToy(){if(toy!=null){toy.Close();toy.Dispose();toy=null;}}
    void CreateToy(int kind)
    {
        RemoveToy();Wake();paused=false;pause.Text="Pausar";UpdateTrayText();
        Rectangle r=Screen.FromPoint(CatPoint).WorkingArea;
        Point p=new Point(Math.Max(r.Left+20,Math.Min(r.Right-20,(int)neko.X+120)),Math.Max(r.Top+20,Math.Min(r.Bottom-20,(int)neko.Y)));
        toy=new ToyWindow(kind,p);toy.Show(this);Notice(kind==4?"Mueve el cursor para jugar con el láser.":"Arrastra el juguete para lanzarlo. Los gatos lo perseguirán.");
    }
    void AddCat(int index,bool userAction)
    {
        if(slot!=0)return;
        if(index>=4){if(userAction)Notice("Puedes tener hasta cuatro gatos.");return;}
        CatForm cat=null;
        try {
            cat=new CatForm(index,this);cat.Show(this);companions.Add(cat);
            if(userAction){settings.CatCount=companions.Count+1;Save();Notice("Abre el icono del nuevo gato para cambiar su nombre, diseño y accesorios.");}
        }catch(Exception ex){if(cat!=null)cat.Dispose();settings.CatCount=companions.Count+1;Save();MessageBox.Show("No pude agregar el gato.\n\n"+ex.Message,"NekoCat By Mauro",MessageBoxButtons.OK,MessageBoxIcon.Information);}
    }
    void RemoveLastCat()
    {
        if(slot!=0){leader.RemoveLastCat();return;}
        if(companions.Count==0){Notice("Ya queda un solo gato.");return;}
        CatForm cat=companions[companions.Count-1];companions.RemoveAt(companions.Count-1);
        cat.Close();cat.Dispose();settings.CatCount=companions.Count+1;Save();
    }
    void Place(int kind,Point at)
    {
        PetPlace old;if(places.TryGetValue(kind,out old)){old.Close();old.Dispose();places.Remove(kind);}
        var place=new PetPlace(kind,at,SavePlaces);places[kind]=place;place.Show(this);SavePlaces();
    }
    void ClearPlaces(){foreach(PetPlace item in places.Values){item.Close();item.Dispose();}places.Clear();SavePlaces();}
    void SavePlaces()
    {
        var s=new System.Text.StringBuilder();foreach(var pair in places){Point p=pair.Value.Center;s.Append(pair.Key).Append(':').Append(p.X).Append(':').Append(p.Y).Append(';');}
        settings.Objects=s.ToString();Save();
    }
    void LoadPlaces()
    {
        foreach(string token in (settings.Objects??"").Split(';')){
            string[] bits=token.Split(':');int kind,x,y;
            if(bits.Length==3 && int.TryParse(bits[0],out kind) && int.TryParse(bits[1],out x) && int.TryParse(bits[2],out y) && kind>=0 && kind<PetPlace.Names.Length){
                Rectangle v=SystemInformation.VirtualScreen;
                if(v.Contains(x,y))Place(kind,new Point(x,y));
            }
        }
    }
    bool StartSleep(bool manual)
    {
        IntPtr found=WindowPerch.Find(CatPoint,Side);
        if(found==IntPtr.Zero)return false;
        perch=found;explicitSleep=manual;sleepTicks=0;sleepMouse=Cursor.Position;
        Rectangle r;WindowPerch.Bounds(found,out r);
        perchFraction=Math.Max(0.1,Math.Min(0.9,(neko.X-r.Left)/Math.Max(1,r.Width)));
        if(!FollowPerch())return false;
        RemoveToy();
        if(manual){paused=false;pause.Text="Pausar";UpdateTrayText();}
        DrawCat();return true;
    }
    bool FollowPerch()
    {
        Rectangle r;
        if(!WindowPerch.Bounds(perch,out r)){Wake();return false;}
        Rectangle area=Screen.FromRectangle(r).WorkingArea;
        if(r.Top<area.Top+Side || r.Top>area.Bottom || r.Right<area.Left || r.Left>area.Right){Wake();return false;}
        neko.Sprite=sleepTicks<8?"tired":"sleeping";neko.SpriteFrame=sleepTicks/4;
        Bitmap frame=GetFrame();int bottom=frame.Height-1;
        while(bottom>0){bool opaque=false;for(int x=0;x<frame.Width;x++)if(frame.GetPixel(x,bottom).A>0){opaque=true;break;}if(opaque)break;bottom--;}
        neko.X=Math.Max(area.Left+Side/2,Math.Min(area.Right-Side/2,r.Left+r.Width*perchFraction));
        neko.Y=r.Top+Side/2-bottom-1;
        // Leave the perch if a different foreground window covers the sleeping cat.
        IntPtr foreground=Native.GetForegroundWindow();uint pid;Native.GetWindowThreadProcessId(foreground,out pid);
        Rectangle front;
        if(foreground!=perch && pid!=Native.GetCurrentProcessId() && WindowPerch.Bounds(foreground,out front) && front.Contains(CatPoint)){Wake();return false;}
        return true;
    }
    void TickCat()
    {
        if(renaming || menu.Visible)return;
        ticks=(ticks+1)%1000000;if(heartTicks>0)heartTicks--;
        if(giftTicks>0)giftTicks--;
        Point mouse=Cursor.Position;
        if(paused){lastMouse=mouse;DrawBadge();return;}
        if(slot==0)foreach(PetPlace p in places.Values)p.Step();
        if(kickCooldown>0)kickCooldown--;
        if(placeCooldown>0)placeCooldown--;
        if(settings.Surprises && ticks%280==0 && random.Next(4)==0){
            string[] gifts={"★ Encontró una estrella","✿ Encontró una flor","♥ Encontró un corazón","◆ Encontró una joyita"};
            gift=gifts[random.Next(gifts.Length)];giftTicks=65;if(settings.Sounds)PetAudio.Play(false);
        }
        if(petTicks>0){petTicks--;neko.Sprite="scratchSelf";neko.SpriteFrame=ticks/2;DrawCat();lastMouse=mouse;return;}
        if(boxTicks>0){boxTicks--;neko.Sprite="sleeping";neko.SpriteFrame=ticks/4;DrawCat();if(boxTicks==0){stretchTicks=10;sleepAtKind=-1;}lastMouse=mouse;return;}
        if(stretchTicks>0){stretchTicks--;neko.Sprite="tired";neko.SpriteFrame=0;DrawCat();lastMouse=mouse;return;}
        if(edgeTicks>0){edgeTicks--;neko.Sprite="alert";neko.SpriteFrame=0;DrawCat();lastMouse=mouse;return;}
        if(petMode.Checked && Math.Abs(mouse.X-neko.X)<Side/2+10 && Math.Abs(mouse.Y-neko.Y)<Side/2+10 && perch==IntPtr.Zero){
            neko.Sprite="idle";neko.SpriteFrame=0;DrawCat();lastMouse=mouse;return;
        }
        if(perch!=IntPtr.Zero){
            sleepTicks++;
            bool moved=Math.Abs(mouse.X-sleepMouse.X)+Math.Abs(mouse.Y-sleepMouse.Y)>100;
            if(!explicitSleep && (sleepTicks>220 || (!settings.Independent && moved)))Wake();
            if(perch!=IntPtr.Zero && FollowPerch()){DrawCat();lastMouse=mouse;return;}
        }
        ToyWindow activeToy=leader.toy;
        if(activeToy!=null){
            if(slot==0)activeToy.Step();Rectangle r=Screen.FromPoint(new Point((int)activeToy.X,(int)activeToy.Y)).WorkingArea;ClampTo(r);
            neko.StopDistance=Side/2+10;
            neko.Tick(activeToy.X,activeToy.Y,r.Left,r.Top,r.Right,r.Bottom);
            double dx=activeToy.X-neko.X,dy=activeToy.Y-neko.Y,dist=Math.Sqrt(dx*dx+dy*dy);
            if(!activeToy.Dragging && dist<Side/2+24 && kickCooldown==0){
                double angle=dist<1?random.NextDouble()*Math.PI*2:Math.Atan2(dy,dx)+(random.NextDouble()-0.5)*1.3;
                activeToy.VX=Math.Cos(angle)*16;activeToy.VY=Math.Sin(angle)*16;kickCooldown=10;
            }
            DrawCat();lastMouse=mouse;return;
        }
        Rectangle area=Screen.FromPoint(settings.Independent?CatPoint:mouse).WorkingArea;ClampTo(area);
        neko.StopDistance=settings.Independent?14*settings.Scale:48*settings.Scale;
        double oldX=neko.X,oldY=neko.Y;
        if(settings.KeyboardReaction && ticks%5==0 && KeyboardBusy() && Native.GetForegroundWindow()!=Handle){
            if(settings.Independent){wanderGoal=mouse;hasGoal=true;wanderWait=0;}
            if(ticks-lastTypingTick>=300 || ticks<lastTypingTick){lastTypingTick=ticks;gift="⌨ ¿Qué escribes?";giftTicks=25;}
        }
        PetPlace goal=destination;
        if(goal!=null && (goal.IsDisposed || goal.EmptyTicks>0 || goal.Dragging || placeCooldown>0)){goal=null;destination=null;}
        if(goal==null && (settings.Independent || mouse==lastMouse) && placeCooldown==0 && leader.places.Count>0 && ticks%120==0){
            double best=double.MaxValue;
            foreach(PetPlace p in leader.places.Values){
                if(p.EmptyTicks>0 || p.Dragging)continue;
                Point pos=p.Center;double dx=pos.X-neko.X,dy=pos.Y-neko.Y,d=dx*dx+dy*dy;
                if(d<best){best=d;goal=p;}
            }
            destination=goal;
        }
        if(goal!=null){
            Point goalPosition=goal.Center;Rectangle targetArea=Screen.FromPoint(goalPosition).WorkingArea;neko.StopDistance=18*settings.Scale;
            neko.Tick(goalPosition.X,goalPosition.Y,targetArea.Left,targetArea.Top,targetArea.Right,targetArea.Bottom);
            if(Math.Abs(neko.X-goalPosition.X)+Math.Abs(neko.Y-goalPosition.Y)<Side/2+20){
                destination=null;
                placeCooldown=goal.Kind<=1?500:280;
                if(goal.Kind<=1){goal.Use();if(settings.Sounds)PetAudio.Play(true);}
                else {boxTicks=goal.Kind==2?80:goal.Kind==3?60:95;sleepAtKind=goal.Kind;neko.X=goalPosition.X;neko.Y=goalPosition.Y;}
            }
            DrawCat();lastMouse=mouse;return;
        }
        if(settings.Independent){
            if(!hasGoal && wanderWait<=0){
                wanderGoal=new Point(random.Next(area.Left+Side/2,Math.Max(area.Left+Side/2+1,area.Right-Side/2)),random.Next(area.Top+Side/2,Math.Max(area.Top+Side/2+1,area.Bottom-Side/2)));hasGoal=true;
            }
            if(hasGoal){
                neko.Tick(wanderGoal.X,wanderGoal.Y,area.Left,area.Top,area.Right,area.Bottom);
                if(Math.Abs(neko.X-wanderGoal.X)+Math.Abs(neko.Y-wanderGoal.Y)<neko.StopDistance*1.5){hasGoal=false;wanderWait=random.Next(25,settings.DailyRoutine && (DateTime.Now.Hour>=22 || DateTime.Now.Hour<7)?130:65);}
            }else{wanderWait--;neko.Tick(neko.X,neko.Y,area.Left,area.Top,area.Right,area.Bottom);}
        }else neko.Tick(mouse.X,mouse.Y,area.Left,area.Top,area.Right,area.Bottom);
        if(settings.EdgePeek && ticks%450==0 && (neko.X<area.Left+Side || neko.X>area.Right-Side)){
            neko.X=neko.X<area.Left+Side?area.Left+Side/4:area.Right-Side/4;edgeTicks=9;
        }
        bool resting=Math.Abs(neko.X-oldX)+Math.Abs(neko.Y-oldY)<0.5;
        idleTicks=resting && (settings.Independent || mouse==lastMouse)?idleTicks+1:0;
        bool night=settings.DailyRoutine && (DateTime.Now.Hour>=22 || DateTime.Now.Hour<7);
        if(settings.AutoSleep && ((settings.Independent && ticks%(night?200:450)==0) || idleTicks>=(night?80:150))){
            if(!StartSleep(false))idleTicks=0;
        }
        DrawCat();lastMouse=mouse;
    }
    bool KeyboardBusy()
    {
        for(int key=0x41;key<=0x5A;key++)if((Native.GetAsyncKeyState(key)&0x8000)!=0)return true;
        return false;
    }
    Bitmap GetFrame()
    {
        Point[] list=sprites[neko.Sprite];Point cell=list[neko.SpriteFrame%list.Length];string key=cell.X+","+cell.Y;
        Bitmap b;if(frames.TryGetValue(key,out b))return b;
        b=new Bitmap(Side,Side,PixelFormat.Format32bppPArgb);
        using(Graphics g=Graphics.FromImage(b)){
            int cellSize=SkinFactory.FrameSize(settings.Design);
            g.CompositingMode=CompositingMode.SourceCopy;
            g.InterpolationMode=settings.Design>=13 && settings.Design<=15 ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode=PixelOffsetMode.Half;
            using(Bitmap tile=sheet.Clone(new Rectangle(cell.X*cellSize,cell.Y*cellSize,cellSize,cellSize),PixelFormat.Format32bppArgb))
                g.DrawImage(tile,new Rectangle(0,0,Side,Side),0,0,cellSize,cellSize,GraphicsUnit.Pixel);
            if(settings.Accessory!=0){
                g.CompositingMode=CompositingMode.SourceOver;
                float s=Side/32f;using(var red=new SolidBrush(Color.FromArgb(231,90,116)))using(var gold=new SolidBrush(Color.FromArgb(251,197,52)))
                using(var dark=new Pen(Color.FromArgb(64,49,70),Math.Max(1,s*2))){
                    if(settings.Accessory==1){g.FillEllipse(red,4*s,2*s,8*s,7*s);g.FillEllipse(red,19*s,2*s,8*s,7*s);g.FillEllipse(gold,14*s,4*s,5*s,5*s);}
                    else if(settings.Accessory==2){g.FillRectangle(red,9*s,0,15*s,6*s);g.FillRectangle(red,5*s,5*s,22*s,3*s);}
                    else if(settings.Accessory==3){g.FillPolygon(gold,new PointF[]{new PointF(5*s,7*s),new PointF(7*s,0),new PointF(12*s,5*s),new PointF(17*s,0),new PointF(22*s,5*s),new PointF(26*s,0),new PointF(27*s,7*s)});}
                    else if(settings.Accessory==4){g.DrawEllipse(dark,6*s,10*s,9*s,7*s);g.DrawEllipse(dark,18*s,10*s,9*s,7*s);g.DrawLine(dark,15*s,13*s,18*s,13*s);}
                }
            }
        }frames.Add(key,b);return b;
    }
    void DrawCat(){
        if(boxTicks>0 && sleepAtKind==2){using(var empty=new Bitmap(Side,Side,PixelFormat.Format32bppPArgb))Present(empty,(int)neko.X-Side/2,(int)neko.Y-Side/2);badge.Hide();return;}
        Present(GetFrame(),(int)Math.Round(neko.X)-Side/2,(int)Math.Round(neko.Y)-Side/2);DrawBadge();
    }
    void DrawBadge()
    {
        if(!settings.ShowName && heartTicks==0 && giftTicks==0){badge.Hide();return;}
        using(var b=new Bitmap(190,58,PixelFormat.Format32bppPArgb))using(Graphics g=Graphics.FromImage(b)){
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            if(settings.ShowName || giftTicks>0){
                using(var font=new Font("Segoe UI",9,FontStyle.Bold))using(var bg=new SolidBrush(Color.FromArgb(225,45,38,47)))using(var fg=new SolidBrush(Color.White)){
                    string label=giftTicks>0?gift:settings.Name;SizeF measured=g.MeasureString(label,font);float w=Math.Min(184,measured.Width+16);
                    g.FillRectangle(bg,(190-w)/2,33,w,23);g.DrawString(label,font,fg,new RectangleF((190-w)/2+8,36,w-12,19));
                }
            }
            if(heartTicks>0){
                for(int i=0;i<3;i++){
                    int x=60+i*27,y=5+(i%2)*5-(25-heartTicks)%5;
                    using(var brush=new SolidBrush(Color.FromArgb(235,238,104,146))){
                        g.FillEllipse(brush,x,y,9,9);g.FillEllipse(brush,x+7,y,9,9);g.FillPolygon(brush,new Point[]{new Point(x,y+5),new Point(x+16,y+5),new Point(x+8,y+17)});
                    }
                }
            }
            Rectangle r=Screen.FromPoint(CatPoint).WorkingArea;
            int xPos=Math.Max(r.Left,Math.Min(r.Right-190,(int)neko.X-95));
            int yPos=(int)neko.Y-Side/2-58;if(yPos<r.Top)yPos=(int)neko.Y+Side/2+2;
            if(!badge.Visible)badge.Show(this);badge.Present(b,xPos,yPos);
        }
    }
    protected override void Dispose(bool disposing)
    {
        if(disposing && !cleaned){cleaned=true;timer.Stop();timer.Dispose();if(slot==0){foreach(CatForm c in companions)c.Dispose();companions.Clear();foreach(PetPlace p in places.Values)p.Dispose();places.Clear();}RemoveToy();badge.Dispose();tray.Visible=false;tray.Dispose();menu.Dispose();ClearFrames();if(catIcon!=null)catIcon.Dispose();if(sheet!=null)sheet.Dispose();if(original!=null)original.Dispose();}
        base.Dispose(disposing);
    }
}
internal sealed class WelcomeToast : Form
{
    readonly System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer();
    readonly Icon icon;
    public WelcomeToast(Icon catIcon)
    {
        icon = (Icon)catIcon.Clone();
        Text = "NekoCat By Mauro 2.0.1";
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(252,247,240);
        ClientSize = new Size(360,82);
        DoubleBuffered = true;
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(area.Right - Width - 20,area.Bottom - Height - 20);
        closeTimer.Interval = 3000;
        closeTimer.Tick += delegate { Close(); };
        Shown += delegate { closeTimer.Start(); };
        MouseClick += delegate { Close(); };
    }
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams
    {
        get { CreateParams p = base.CreateParams; p.ExStyle |= 0x80 | 0x08000000; return p; }
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; }
        base.WndProc(ref m);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.DrawIcon(icon,new Rectangle(17,25,32,32));
        using (var title = new Font("Segoe UI",13,FontStyle.Bold))
        using (var caption = new Font("Segoe UI",9))
        using (var ink = new SolidBrush(Color.FromArgb(51,43,43)))
        using (var muted = new SolidBrush(Color.FromArgb(116,104,104)))
        using (var border = new Pen(Color.FromArgb(228,212,198)))
        {
            e.Graphics.DrawString("NekoCat By Mauro 2.0.1",title,ink,64,18);
            e.Graphics.DrawString("Tu mascota ya está aquí.",caption,muted,66,46);
            e.Graphics.DrawRectangle(border,0,0,Width-1,Height-1);
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { closeTimer.Dispose(); icon.Dispose(); }
        base.Dispose(disposing);
    }
}
