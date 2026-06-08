using System.Windows;
using Noty4Me.Models;
using Noty4Me.Services;

namespace Noty4Me.UI;

public partial class SettingsWindow : Window
{
    private static SettingsWindow? _instance;
    private bool _loaded;

    private SettingsWindow()
    {
        InitializeComponent();
        WindowChrome.Attach(this);

        FlavorBox.ItemsSource = ThemeManager.AllFlavors;
        AccentBox.ItemsSource = ThemeManager.AllAccents;

        var cfg = App.State.Config;
        FlavorBox.SelectedItem = cfg.Flavor;
        AccentBox.SelectedItem = cfg.Accent;
        StartMinChk.IsChecked = cfg.StartMinimized;
        StartWithWinChk.IsChecked = AutostartService.IsEnabled();
        _loaded = true;
    }

    public static void ShowOrFocus()
    {
        if (_instance is null)
        {
            _instance = new SettingsWindow();
            _instance.Closed += (_, _) => _instance = null;
            _instance.Show();
        }
        else
        {
            _instance.Activate();
        }
    }

    private void OnFlavorChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_loaded || FlavorBox.SelectedItem is not CatFlavor f) return;
        App.State.Config.Flavor = f;
        ThemeManager.Apply(f, App.State.Config.Accent);
        ConfigStore.Save(App.State.Config);
    }

    private void OnAccentChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_loaded || AccentBox.SelectedItem is not CatAccent a) return;
        App.State.Config.Accent = a;
        ThemeManager.Apply(App.State.Config.Flavor, a);
        ConfigStore.Save(App.State.Config);
    }

    private void OnStartMinChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        App.State.Config.StartMinimized = StartMinChk.IsChecked == true;
        ConfigStore.Save(App.State.Config);
    }

    private void OnStartWithWinChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        var wantOn = StartWithWinChk.IsChecked == true;
        if (!AutostartService.TrySetEnabled(wantOn, out var msg))
        {
            // Revert checkbox to actual OS state and surface why.
            _loaded = false;
            StartWithWinChk.IsChecked = AutostartService.IsEnabled();
            _loaded = true;
            MessageBox.Show(this, msg ?? "Could not change autostart setting.",
                "Start with Windows", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
