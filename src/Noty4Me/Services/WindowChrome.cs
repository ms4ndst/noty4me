using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Noty4Me.Models;

namespace Noty4Me.Services;

// Themes the native non-client area (title bar) via DWM so the window
// chrome matches the active Catppuccin flavor instead of staying white.
public static class WindowChrome
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;
    private const int DWMWA_BORDER_COLOR = 34;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private static readonly HashSet<IntPtr> _tracked = new();
    private static bool _hooked;

    public static void Attach(Window w)
    {
        w.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(w).Handle;
            if (hwnd != IntPtr.Zero) _tracked.Add(hwnd);
            ApplyAll();
        };
        w.Closed += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(w).Handle;
            if (hwnd != IntPtr.Zero) _tracked.Remove(hwnd);
        };

        if (!_hooked)
        {
            ThemeManager.ThemeChanged += ApplyAll;
            _hooked = true;
        }
    }

    private static void ApplyAll()
    {
        var isLight = ThemeManager.CurrentFlavor == CatFlavor.Latte;
        var capColor = ToBgr(ThemeManager.GetCurrentColor("Cat.Mantle"));
        var txtColor = ToBgr(ThemeManager.GetCurrentColor("Cat.Text"));
        var brdColor = ToBgr(ThemeManager.GetCurrentColor("Cat.Surface0"));
        int darkMode = isLight ? 0 : 1;

        foreach (var hwnd in _tracked)
        {
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref capColor, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref txtColor, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref brdColor, sizeof(int));
        }
    }

    private static int ToBgr(Color c) => c.R | (c.G << 8) | (c.B << 16);
}
