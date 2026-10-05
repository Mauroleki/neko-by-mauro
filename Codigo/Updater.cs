using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

// Separate process: Windows does not replace a running executable.
internal static class Updater
{
    [STAThread] static void Main(string[] args)
    {
        string target=null;
        try {
            if(args.Length!=4)throw new ArgumentException("Faltan datos de la actualización.");
            target=Path.GetFullPath(args[0]);string staged=Path.GetFullPath(args[1]);
            int parent=int.Parse(args[2]);string expected=args[3].ToLowerInvariant();
            if((Path.GetFileName(target)!="Oneko.exe" && Path.GetFileName(target)!="NekoCat.exe") || !File.Exists(target) || !File.Exists(staged) || expected.Length!=64)
                throw new InvalidDataException("La actualización no tiene los archivos esperados.");
            using(var stream=File.OpenRead(staged))using(var sha=SHA256.Create()){
                string actual=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
                if(actual!=expected)throw new InvalidDataException("La verificación del ejecutable falló.");
            }
            try { using(var process=Process.GetProcessById(parent))process.WaitForExit(30000); }
            catch(ArgumentException) { /* The old process has already exited. */ }
            string system=Path.Combine(Path.GetDirectoryName(target),"Sistema");
            Directory.CreateDirectory(system);
            string pending=Path.Combine(system,"Oneko.pending.exe");
            string backup=Path.Combine(system,"Oneko.previous.exe");
            File.Copy(staged,pending,true);
            if(File.Exists(backup))File.Delete(backup);
            Exception last=null;
            for(int i=0;i<20;i++){
                try { File.Replace(pending,target,backup,true);last=null;break; }
                catch(IOException ex){last=ex;Thread.Sleep(500);}
            }
            if(last!=null)throw last;
            try { File.Delete(staged); }catch(IOException){}
            Process.Start(new ProcessStartInfo(target){WorkingDirectory=Path.GetDirectoryName(target),UseShellExecute=true});
        }catch(Exception ex){
            MessageBox.Show("No se pudo instalar la actualización. La versión anterior se conserva.\n\n"+ex.Message,
                "NekoCat By Mauro",MessageBoxButtons.OK,MessageBoxIcon.Error);
            if(target!=null && File.Exists(target))try { Process.Start(new ProcessStartInfo(target){WorkingDirectory=Path.GetDirectoryName(target),UseShellExecute=true}); }catch(Exception){}
        }
    }
}
