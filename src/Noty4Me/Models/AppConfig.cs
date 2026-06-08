namespace Noty4Me.Models;

public enum CatFlavor { Latte, Frappe, Macchiato, Mocha }

public enum CatAccent
{
    Rosewater, Flamingo, Pink, Mauve, Red, Maroon, Peach,
    Yellow, Green, Teal, Sky, Sapphire, Blue, Lavender
}

public sealed class AppConfig
{
    public CatFlavor Flavor { get; set; } = CatFlavor.Mocha;
    public CatAccent Accent { get; set; } = CatAccent.Mauve;
    public bool StartMinimized { get; set; } = true;
}
