using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class UpdateManager
{
    static bool busy;
    static Version lastPromptedVersion;
    static System.Windows.Forms.Timer watchTimer;
    const string OfficialRepository="https://github.com/Mauroleki/neko-by-mauro";
    static string RootPath { get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); } }
    static string ChannelPath { get { return Path.Combine(RootPath,"Sistema","canal.txt"); } }
    static string UpdateDirectory { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OnekoByMau","Updates"); } }

    internal sealed class Release
    {
        public Version Version;
        public Uri Url;
        public string Digest;
        public long Size;
    }

    internal static bool TryRepository(string text,out string owner,out string repo)
    {
        owner=repo=null;Uri uri;
        if(!Uri.TryCreate(text == null ? "" : text.Trim(),UriKind.Absolute,out uri) || uri.Scheme!="https" ||
           !uri.Host.Equals("github.com",StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))return false;
        string[] parts=uri.AbsolutePath.Trim('/').Split('/');
        if(parts.Length!=2 || !Regex.IsMatch(parts[0],@"^[A-Za-z0-9_.-]+$") || !Regex.IsMatch(parts[1],@"^[A-Za-z0-9_.-]+$"))return false;
        owner=parts[0];repo=parts[1];return true;
    }
    internal static bool HasChannel()
    {
        string owner,repo;return ReadChannel(out owner,out repo);
    }
    static bool ReadChannel(out string owner,out string repo)
    {
        owner=repo=null;
        try {
            if(File.Exists(ChannelPath) && TryRepository(File.ReadAllText(ChannelPath),out owner,out repo))return true;
        }catch(IOException){}catch(UnauthorizedAccessException){}
        return TryRepository(OfficialRepository,out owner,out repo);
    }
    static string EnsureHelper()
    {
        string bundled=Path.Combine(RootPath,"Sistema","Updater.exe");
        if(File.Exists(bundled))return bundled;
        Directory.CreateDirectory(UpdateDirectory);
        string extracted=Path.Combine(UpdateDirectory,"Updater-"+Guid.NewGuid().ToString("N")+".exe");
        using(Stream resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("Updater.exe")) {
            if(resource==null)throw new FileNotFoundException("Falta el actualizador integrado. Descarga el ZIP completo.");
            using(var output=File.Create(extracted))resource.CopyTo(output);
        }
        return extracted;
    }
    internal static Release ParseRelease(string json,string owner,string repo)
    {
        var data=JsonLite.Parse(json) as Dictionary<string,object>;
        if(data==null || !data.ContainsKey("tag_name") || !data.ContainsKey("assets"))throw new InvalidDataException("Respuesta de versión incompleta.");
        string tag=Convert.ToString(data["tag_name"]).TrimStart('v','V');Version version;
        if(!Version.TryParse(tag,out version))throw new InvalidDataException("La versión publicada no tiene formato v1.6.0.");
        List<object> assets=data["assets"] as List<object>;
        if(assets==null)throw new InvalidDataException("La versión publicada no contiene archivos.");
        foreach(object item in assets){
            var asset=item as Dictionary<string,object>;
            if(asset==null || !asset.ContainsKey("name") || Convert.ToString(asset["name"])!="NekoCat.exe")continue;
            string digest=Convert.ToString(asset["digest"]);string download=Convert.ToString(asset["browser_download_url"]);
            Uri url;long size=Convert.ToInt64(asset["size"]);
            string prefix="/"+owner+"/"+repo+"/releases/download/";
            if(!Uri.TryCreate(download,UriKind.Absolute,out url) || url.Scheme!="https" ||
               !url.Host.Equals("github.com",StringComparison.OrdinalIgnoreCase) ||
               !url.AbsolutePath.StartsWith(prefix,StringComparison.OrdinalIgnoreCase) ||
               !url.AbsolutePath.EndsWith("/NekoCat.exe",StringComparison.OrdinalIgnoreCase) ||
               size<10000 || size>30*1024*1024 || digest==null || !Regex.IsMatch(digest,@"^sha256:[0-9a-fA-F]{64}$"))
                throw new InvalidDataException("El archivo publicado no tiene una descarga o una huella SHA-256 válida.");
            return new Release {Version=version,Url=url,Digest=digest.Substring(7).ToLowerInvariant(),Size=size};
        }
        throw new InvalidDataException("La versión publicada debe incluir NekoCat.exe como archivo adjunto.");
    }
    static string Hash(string file)
    {
        using(var stream=File.OpenRead(file))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
    }
    internal static void StartWatching(Form window)
    {
        if(watchTimer!=null)return;
        watchTimer=new System.Windows.Forms.Timer {Interval=15*60*1000};
        watchTimer.Tick+=async delegate { await CheckAsync(window,true); };
        window.Disposed+=delegate {
            if(watchTimer!=null){watchTimer.Stop();watchTimer.Dispose();watchTimer=null;}
        };
        watchTimer.Start();
        window.BeginInvoke(new Action(async delegate { await CheckAsync(window,true); }));
    }
    public static async Task CheckAsync(Form window,bool automatic)
    {
        if(busy || window.IsDisposed || window.Disposing)return;
        string owner,repo;
        if(!ReadChannel(out owner,out repo)){
            if(!automatic)MessageBox.Show("Todavía no se ha configurado el repositorio de actualizaciones. Consulta LEEME.txt.","NekoCat By Mauro",MessageBoxButtons.OK,MessageBoxIcon.Information);
            return;
        }
        busy=true;
        try {
            ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
            string api="https://api.github.com/repos/"+owner+"/"+repo+"/releases/latest";
            string json;
            using(var client=new WebClient()){
                client.Headers[HttpRequestHeader.UserAgent]="Neko-By-Mauro-Updater/1.0";
                client.Headers[HttpRequestHeader.Accept]="application/vnd.github+json";
                json=await client.DownloadStringTaskAsync(new Uri(api));
            }
            Release release=ParseRelease(json,owner,repo);
            if(window.IsDisposed || window.Disposing)return;
            var installed=Assembly.GetExecutingAssembly().GetName().Version;
            if(release.Version<=installed){
                if(!automatic)MessageBox.Show("Ya tienes la última versión ("+installed.Major+"."+installed.Minor+").","NekoCat By Mauro",MessageBoxButtons.OK,MessageBoxIcon.Information);
                return;
            }
            if(automatic && release.Version==lastPromptedVersion)return;
            lastPromptedVersion=release.Version;
            if(MessageBox.Show(window,"Hay una nueva versión disponible.\n\n¿Quieres actualizar?\n\nVersión "+release.Version,
                "NekoCat By Mauro",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            Directory.CreateDirectory(UpdateDirectory);
            string staged=Path.Combine(UpdateDirectory,"Oneko-"+Guid.NewGuid().ToString("N")+".exe");
            try {
                using(var client=new WebClient())await client.DownloadFileTaskAsync(release.Url,staged);
                if(new FileInfo(staged).Length!=release.Size || Hash(staged)!=release.Digest)
                    throw new InvalidDataException("La descarga no coincide con la huella SHA-256 publicada. No se instalará.");
                string helper=EnsureHelper();
                string target=Application.ExecutablePath;
                string args="\""+target+"\" \""+staged+"\" "+Process.GetCurrentProcess().Id+" "+release.Digest;
                Process.Start(new ProcessStartInfo(helper,args){UseShellExecute=false,WorkingDirectory=RootPath});
                window.Close();
            }catch { try { if(File.Exists(staged))File.Delete(staged); }catch(IOException){} throw; }
        }catch(Exception ex){
            if(!automatic || !(ex is WebException))MessageBox.Show("No se pudo actualizar. NekoCat seguirá funcionando.\n\n"+ex.Message,"NekoCat By Mauro",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }finally{busy=false;}
    }
}
