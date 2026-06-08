using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace Noty4Me.UI;

public sealed class TrayIconHost : IDisposable
{
    private readonly WinForms.NotifyIcon _notify;
    private readonly WinForms.ContextMenuStrip _menu;

    public TrayIconHost()
    {
        _menu = new WinForms.ContextMenuStrip();
        _menu.Items.Add("Open notes", null, (_, _) => NotesWindow.ShowOrFocus());
        _menu.Items.Add("Settings...", null, (_, _) => SettingsWindow.ShowOrFocus());
        _menu.Items.Add(new WinForms.ToolStripSeparator());
        _menu.Items.Add("Exit", null, (_, _) => Application.Current.Shutdown());

        _notify = new WinForms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Noty4Me",
            Visible = false,
            ContextMenuStrip = _menu
        };

        _notify.MouseClick += (_, e) =>
        {
            if (e.Button == WinForms.MouseButtons.Left)
                NotesWindow.ShowOrFocus();
        };
        _notify.DoubleClick += (_, _) => NotesWindow.ShowOrFocus();
    }

    public void Show() => _notify.Visible = true;

    private static Icon LoadIcon()
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceUri = new Uri("pack://application:,,,/Assets/tray.ico", UriKind.Absolute);
        try
        {
            var sri = Application.GetResourceStream(resourceUri);
            if (sri is not null)
            {
                using var s = sri.Stream;
                return new Icon(s);
            }
        }
        catch { /* fall through to default */ }
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        _notify.Visible = false;
        _notify.Dispose();
        _menu.Dispose();
    }
}
