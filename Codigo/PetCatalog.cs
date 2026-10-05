using System;using System.IO;using System.Drawing;using System.Reflection;
internal static class PetCatalog {
 public static readonly string[] Names = {
  "Blanco clásico",
  "Naranjita",
  "Gris humo",
  "Negrito",
  "Crema",
  "Rosa pastel",
  "Lavanda",
  "Azul cielo",
  "Siamés",
  "Atigrado naranja",
  "Atigrado gris",
  "Calicó",
  "Esmoquin",
  "Karnalito (VALORANT)",
  "El Caballero (Hollow Knight)",
  "Hornet (Hollow Knight)",
  "Charmander",
  "Kirby",
  "Appa",
  "Eren Jaeger",
  "Toji Fushiguro",
  "Totoro",
  "TurboAbuela Gato",
  "Hatsune Miku",
  "Kasane Teto",
  "Among Us Rojo",
  "Barbaro",
  "Breach",
  "Brimstone",
  "Chamber",
  "Clove",
  "Cypher",
  "Duende Lanzadardos",
  "Fade",
  "Gekko",
  "Harbor",
  "Iso",
  "Jeff",
  "Jett",
  "KAYO",
  "Killjoy",
  "Miks",
  "Mini PEKKA",
  "Montapuercos",
  "Neon",
  "Omen",
  "Phoenix",
  "Raze",
  "Reyna",
  "Sage",
  "Tails",
  "Tejo",
  "Waylay",
  "Yoru"
 };
 public static readonly string[] Categories = {
  "Otros",
  "Otros",
  "Otros",
  "Otros",
  "Otros",
  "Otros",
  "Otros",
  "Otros",
  "Otros",
  "Otros",
  "Otros",
  "Otros",
  "Otros",
  "Valorant",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Anime",
  "Anime",
  "Anime",
  "Anime",
  "Anime",
  "Otros",
  "Otros",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos",
  "Videojuegos"
 };
 public static readonly string[] Resources = {
  null,
  null,
  null,
  null,
  null,
  null,
  null,
  null,
  null,
  null,
  null,
  null,
  null,
  null,
  null,
  null,
  "pets.Videojuegos.Charmander.png",
  "pets.Videojuegos.Kirby.png",
  "pets.Anime.Appa.png",
  "pets.Anime.Eren_Jaeger.png",
  "pets.Anime.Toji_Fushiguro.png",
  "pets.Anime.Totoro.png",
  "pets.Anime.TurboAbuela_Gato.png",
  "pets.Otros.Hatsune_Miku.png",
  "pets.Otros.Kasane_Teto.png",
  "pets.Videojuegos.Among_Us_Rojo.png",
  "pets.Videojuegos.Barbaro.png",
  "pets.Videojuegos.Breach.png",
  "pets.Videojuegos.Brimstone.png",
  "pets.Videojuegos.Chamber.png",
  "pets.Videojuegos.Clove.png",
  "pets.Videojuegos.Cypher.png",
  "pets.Videojuegos.Duende_Lanzadardos.png",
  "pets.Videojuegos.Fade.png",
  "pets.Videojuegos.Gekko.png",
  "pets.Videojuegos.Harbor.png",
  "pets.Videojuegos.Iso.png",
  "pets.Videojuegos.Jeff.png",
  "pets.Videojuegos.Jett.png",
  "pets.Videojuegos.KAYO.png",
  "pets.Videojuegos.Killjoy.png",
  "pets.Videojuegos.Miks.png",
  "pets.Videojuegos.Mini_PEKKA.png",
  "pets.Videojuegos.Montapuercos.png",
  "pets.Videojuegos.Neon.png",
  "pets.Videojuegos.Omen.png",
  "pets.Videojuegos.Phoenix.png",
  "pets.Videojuegos.Raze.png",
  "pets.Videojuegos.Reyna.png",
  "pets.Videojuegos.Sage.png",
  "pets.Videojuegos.Tails.png",
  "pets.Videojuegos.Tejo.png",
  "pets.Videojuegos.Waylay.png",
  "pets.Videojuegos.Yoru.png"
 };
 public static bool HasAtlas(int design){return design>=0&&design<Resources.Length&&Resources[design]!=null;}
 public static Bitmap Load(int design){using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(Resources[design])){if(stream==null)throw new FileNotFoundException("Faltan los sprites de "+Names[design]);using(var source=new Bitmap(stream)){if(source.Width!=1024||source.Height!=512)throw new InvalidDataException("Hoja de sprites incompleta: "+Names[design]);return design>=16?NormalizeWalking(source,design):new Bitmap(source);}}}
 // Remap the movement poses to the engine's direction cells. The provided sheets
 // are pose boards, so their original positions do not match Oneko's atlas layout.
 // Keep the two real back-facing frames for north, two side frames for east/west,
  // and two front-facing frames for south. Other cells (idle, sleep, scratching)
 // are left as supplied.
 static Bitmap NormalizeWalking(Bitmap source,int design){
  var result=new Bitmap(source);
   int bx=0,by=1,b2x=1,b2y=1,sx=1,sy=0,s2x=3,s2y=0,fx=1,fy=2,f2x=3,f2y=3;
   if(design==16){bx=0;by=1;b2x=1;b2y=2;sx=0;sy=3;s2x=2;s2y=3;}
   if(design==17){bx=1;by=1;b2x=1;b2y=3;sx=2;sy=2;s2x=2;s2y=3;fx=0;fy=0;f2x=6;f2y=0;}
   CopyPair(result,source,bx,by,b2x,b2y,1,2,1,3,false); // N
   CopyPair(result,source,bx,by,b2x,b2y,0,2,0,3,false); // NE
   CopyPair(result,source,bx,by,b2x,b2y,1,0,1,1,true);  // NW
   CopyPair(result,source,sx,sy,s2x,s2y,3,0,3,1,false); // E
   CopyPair(result,source,sx,sy,s2x,s2y,4,2,4,3,true);  // W
   CopyPair(result,source,fx,fy,f2x,f2y,6,3,7,2,false); // S
   CopyPair(result,source,fx,fy,f2x,f2y,5,1,5,2,false); // SE
   CopyPair(result,source,fx,fy,f2x,f2y,5,3,6,1,true);  // SW
  return result;
 }
 static void CopyPair(Bitmap result,Bitmap source,int ax,int ay,int bx,int by,int dx1,int dy1,int dx2,int dy2,bool flip){
  CopyPose(result,source,ax,ay,dx1,dy1,flip);CopyPose(result,source,bx,by,dx2,dy2,flip);
 }
 static void CopyPose(Bitmap result,Bitmap source,int sx,int sy,int dx,int dy,bool flip){
  int s=sx*128,d=dx*128,st=sy*128,dt=dy*128;
  for(int y=0;y<128;y++)for(int x=0;x<128;x++)result.SetPixel(d+x,dt+y,source.GetPixel(s+(flip?127-x:x),st+y));
 }
}
