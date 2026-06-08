using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Noty4Me.Models;

namespace Noty4Me.Services;

public static class ThemeManager
{
    private static ResourceDictionary? _currentFlavorDict;

    public static CatFlavor CurrentFlavor { get; private set; } = CatFlavor.Mocha;
    public static CatAccent CurrentAccent { get; private set; } = CatAccent.Mauve;

    public static event Action? ThemeChanged;

    public static void Apply(CatFlavor flavor, CatAccent accent)
    {
        CurrentFlavor = flavor;
        CurrentAccent = accent;

        var app = Application.Current;
        var dicts = app.Resources.MergedDictionaries;

        var newFlavor = new ResourceDictionary { Source = new Uri(FlavorPath(flavor), UriKind.Relative) };

        if (_currentFlavorDict is not null)
            dicts.Remove(_currentFlavorDict);
        dicts.Insert(0, newFlavor);
        _currentFlavorDict = newFlavor;

        RebuildBrushes(newFlavor, accent);
        ThemeChanged?.Invoke();
    }

    private static string FlavorPath(CatFlavor f) => f switch
    {
        CatFlavor.Latte      => "/Themes/Catppuccin.Latte.xaml",
        CatFlavor.Frappe     => "/Themes/Catppuccin.Frappe.xaml",
        CatFlavor.Macchiato  => "/Themes/Catppuccin.Macchiato.xaml",
        CatFlavor.Mocha      => "/Themes/Catppuccin.Mocha.xaml",
        _                    => "/Themes/Catppuccin.Mocha.xaml"
    };

    private static readonly string[] PaletteKeys =
    {
        "Cat.Base","Cat.Mantle","Cat.Crust",
        "Cat.Surface0","Cat.Surface1","Cat.Surface2",
        "Cat.Overlay0","Cat.Overlay1","Cat.Overlay2",
        "Cat.Text","Cat.Subtext0","Cat.Subtext1",
        "Cat.Red","Cat.Green","Cat.Yellow","Cat.Blue","Cat.Mauve","Cat.Lavender",
    };

    private static void RebuildBrushes(ResourceDictionary flavorDict, CatAccent accent)
    {
        var app = Application.Current;

        void Set(string brushKey, string colorKey)
        {
            if (flavorDict[colorKey] is not Color c) return;
            app.Resources[brushKey] = new SolidColorBrush(c);
        }

        Set("Brush.Base",     "Cat.Base");
        Set("Brush.Mantle",   "Cat.Mantle");
        Set("Brush.Crust",    "Cat.Crust");
        Set("Brush.Surface0", "Cat.Surface0");
        Set("Brush.Surface1", "Cat.Surface1");
        Set("Brush.Surface2", "Cat.Surface2");
        Set("Brush.Overlay0", "Cat.Overlay0");
        Set("Brush.Overlay1", "Cat.Overlay1");
        Set("Brush.Text",     "Cat.Text");
        Set("Brush.Subtext0", "Cat.Subtext0");
        Set("Brush.Subtext1", "Cat.Subtext1");
        Set("Brush.Red",      "Cat.Red");
        Set("Brush.Green",    "Cat.Green");

        var accentKey = "Cat." + accent;
        if (flavorDict[accentKey] is Color ac)
            app.Resources["Brush.Accent"] = new SolidColorBrush(ac);
    }

    public static IReadOnlyList<CatFlavor> AllFlavors { get; } = new[]
    {
        CatFlavor.Latte, CatFlavor.Frappe, CatFlavor.Macchiato, CatFlavor.Mocha
    };

    public static IReadOnlyList<CatAccent> AllAccents { get; } = new[]
    {
        CatAccent.Rosewater, CatAccent.Flamingo, CatAccent.Pink, CatAccent.Mauve,
        CatAccent.Red, CatAccent.Maroon, CatAccent.Peach, CatAccent.Yellow,
        CatAccent.Green, CatAccent.Teal, CatAccent.Sky, CatAccent.Sapphire,
        CatAccent.Blue, CatAccent.Lavender
    };

    public static Color GetCurrentColor(string catKey)
    {
        if (_currentFlavorDict?[catKey] is Color c) return c;
        return Colors.Magenta;
    }
}
