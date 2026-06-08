using System;
using System.IO;

namespace Noty4Me.Services;

public static class Paths
{
    public static string ConfigDir { get; }
    public static string NotesFile { get; }
    public static string ConfigFile { get; }

    static Paths()
    {
        // MSIX-packaged: write directly to the package's LocalState so the path
        // is predictable. Without this, writes to %APPDATA% get silently
        // redirected by Package State Redirection to
        //   %LOCALAPPDATA%\Packages\<pfn>\LocalCache\Roaming\Noty4Me\
        // which is hard to find.
        //
        // Unpackaged: %APPDATA%\Noty4Me\ as before.
        if (PackageContext.IsPackaged)
        {
            // ApplicationData.Current.LocalFolder.Path =
            //   C:\Users\<u>\AppData\Local\Packages\<pfn>\LocalState
            ConfigDir = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            ConfigDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Noty4Me");
        }

        NotesFile  = Path.Combine(ConfigDir, "notes.json");
        ConfigFile = Path.Combine(ConfigDir, "config.json");

        Directory.CreateDirectory(ConfigDir);
        MigrateFromPsrIfNeeded();
    }

    public static void EnsureDir() => Directory.CreateDirectory(ConfigDir);

    // One-time migration: existing packaged installs wrote to the PSR-redirected
    // location. Copy those files to the new LocalState location on first launch
    // of this build so users don't lose their notes.
    private static void MigrateFromPsrIfNeeded()
    {
        if (!PackageContext.IsPackaged) return;
        if (File.Exists(NotesFile) || File.Exists(ConfigFile)) return; // already have data

        var legacyRoaming = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Noty4Me");

        // PSR redirect: any GetFolderPath(ApplicationData) read goes through
        // the same redirect as writes, so this path actually resolves to the
        // old location for packaged processes.
        if (!Directory.Exists(legacyRoaming)) return;

        try
        {
            foreach (var src in Directory.EnumerateFiles(legacyRoaming))
            {
                var dst = Path.Combine(ConfigDir, Path.GetFileName(src));
                if (!File.Exists(dst)) File.Copy(src, dst);
            }
        }
        catch
        {
            // Migration is best-effort. The app still runs from a fresh state.
        }
    }
}
