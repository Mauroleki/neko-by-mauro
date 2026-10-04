using System;
using System.Runtime.InteropServices;
using System.Text;
internal static class Native
{
    [StructLayout(LayoutKind.Sequential)] internal struct POINT {public int x,y;public POINT(int a,int b){x=a;y=b;}}
    [StructLayout(LayoutKind.Sequential)] internal struct SIZE {public int cx,cy;public SIZE(int a,int b){cx=a;cy=b;}}
    [StructLayout(LayoutKind.Sequential)] internal struct RECT {public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential,Pack=1)] internal struct BLENDFUNCTION {public byte BlendOp,BlendFlags,SourceConstantAlpha,AlphaFormat;}
    internal delegate bool EnumWindowsProc(IntPtr hwnd,IntPtr param);
    [DllImport("user32.dll")]internal static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")]internal static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")]internal static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")]internal static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
    [DllImport("gdi32.dll")]internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")]internal static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")]internal static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")]internal static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll")]internal static extern bool DestroyIcon(IntPtr icon);
    [DllImport("user32.dll",SetLastError=true)]internal static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dst,ref POINT pos,ref SIZE size,IntPtr src,ref POINT origin,int color,ref BLENDFUNCTION blend,int flags);
    [DllImport("user32.dll")]internal static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")]internal static extern int GetWindowLong(IntPtr hwnd,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongW")]internal static extern int SetWindowLong(IntPtr hwnd,int index,int value);
    [DllImport("user32.dll")]internal static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")]internal static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")]internal static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")]internal static extern bool GetWindowRect(IntPtr hwnd,out RECT rect);
    [DllImport("user32.dll")]internal static extern bool EnumWindows(EnumWindowsProc callback,IntPtr param);
    [DllImport("user32.dll")]internal static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern int GetClassName(IntPtr hwnd,StringBuilder name,int count);
    [DllImport("user32.dll")]internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]internal static extern short GetAsyncKeyState(int key);
    [DllImport("kernel32.dll")]internal static extern uint GetCurrentProcessId();
    [DllImport("dwmapi.dll")]internal static extern int DwmGetWindowAttribute(IntPtr hwnd,int attribute,out int value,int size);
    [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")]internal static extern int DwmGetWindowAttributeRect(IntPtr hwnd,int attribute,out RECT value,int size);
}
