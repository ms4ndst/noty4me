using System;
using System.Threading;
using System.Windows;
using Noty4Me.Services;
using Noty4Me.UI;

namespace Noty4Me;

public partial class App : Application
{
    private const string MutexName = "Noty4Me.SingleInstance.Mutex.v1";
    private Mutex? _mutex;
    private bool _ownsMutex;

    public static AppConfigStateHolder State { get; } = new();
    public static TrayIconHost? Tray { get; private set; }

    private void OnStartup(object sender, StartupEventArgs e)
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out _ownsMutex);
        if (!_ownsMutex)
        {
            Shutdown();
            return;
        }

        State.Config = ConfigStore.Load();
        ThemeManager.Apply(State.Config.Flavor, State.Config.Accent);

        Tray = new TrayIconHost();
        Tray.Show();

        if (!State.Config.StartMinimized)
            NotesWindow.ShowOrFocus();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        Tray?.Dispose();
        if (_ownsMutex)
        {
            try { _mutex?.ReleaseMutex(); } catch { }
        }
        _mutex?.Dispose();
    }
}

public sealed class AppConfigStateHolder
{
    public Noty4Me.Models.AppConfig Config { get; set; } = new();
}
