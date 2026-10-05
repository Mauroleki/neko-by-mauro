@echo off
setlocal
cd /d "%~dp0"
set "NEKO_CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%NEKO_CSC%" set "NEKO_CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%NEKO_CSC%" (
  echo No se encontro el compilador de .NET Framework de Windows.
  echo Esta version requiere Windows 10 u 11 con .NET Framework 4.x.
  pause
  exit /b 1
)
echo Preparando Oneko Escritorio...
"%NEKO_CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /out:"..\Sistema\Updater.exe" /reference:System.dll /reference:System.Windows.Forms.dll Updater.cs
if errorlevel 1 (
  echo No se pudo compilar el actualizador.
  pause
  exit /b 1
)
"%NEKO_CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /out:"..\NekoCat.exe" /win32manifest:app.manifest /resource:..\Recursos\oneko.gif,oneko.gif /resource:..\Recursos\karnalito.png,karnalito.png /resource:..\Recursos\knight.png,knight.png /resource:..\Recursos\hornet.png,hornet.png /resource:..\Recursos\charmander.png,charmander.png /resource:..\Recursos\charmander-back.png,charmander-back.png /resource:..\Recursos\kirby.png,kirby.png /resource:..\Sistema\Updater.exe,Updater.exe @mascotas.rsp /reference:System.dll /reference:System.Xml.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll Oneko.cs NekoEngine.cs Extras.cs Native.cs KarnalitoSprites.cs KnightSprites.cs PetSprites.cs PetCatalog.cs StartupManager.cs World.cs UpdateManager.cs JsonLite.cs
if errorlevel 1 (
  echo No se pudo compilar. Copia el mensaje de arriba para revisar el problema.
  pause
  exit /b 1
)
copy /y "..\NekoCat.exe" "..\Oneko.exe" >nul
echo Listo. Abre NekoCat.exe en la carpeta principal.
pause
