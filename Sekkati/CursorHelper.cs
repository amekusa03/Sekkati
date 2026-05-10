using System;
using System.Runtime.InteropServices;

namespace Sekkati;

internal static class CursorHelper
{
    [DllImport("libX11", EntryPoint = "XOpenDisplay")]
    private static extern IntPtr XOpenDisplay(string? display);

    [DllImport("libX11", EntryPoint = "XCloseDisplay")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11", EntryPoint = "XDefaultRootWindow")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11", EntryPoint = "XQueryPointer")]
    private static extern int XQueryPointer(
        IntPtr display, IntPtr window,
        out IntPtr root, out IntPtr child,
        out int rootX, out int rootY,
        out int winX, out int winY,
        out uint mask);

    // 現在のグローバルマウス座標を返す。取得できない場合は null
    public static (int X, int Y)? TryGetPosition()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return null;
        try
        {
            var display = XOpenDisplay(null);
            if (display == IntPtr.Zero) return null;
            try
            {
                var root = XDefaultRootWindow(display);
                XQueryPointer(display, root,
                    out _, out _, out int rx, out int ry, out _, out _, out _);
                return (rx, ry);
            }
            finally
            {
                XCloseDisplay(display);
            }
        }
        catch
        {
            return null;
        }
    }
}
