using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Noty4Me.Services;

// "Start with Windows" toggle.
//   Packaged (MSIX)   → Windows.ApplicationModel.StartupTask (TaskId from AppxManifest)
//   Unpackaged (dev)  → HKCU\Software\Microsoft\Windows\CurrentVersion\Run
//
// The OS owns the truth in both cases; this service queries/sets it.
public static class AutostartService
{
    private const string TaskId = "Noty4MeAutostart";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "Noty4Me";

    public static bool IsEnabled()
    {
        return PackageContext.IsPackaged ? IsEnabledPackaged() : IsEnabledRegistry();
    }

    public static bool TrySetEnabled(bool enable, out string? message)
    {
        message = null;
        try
        {
            if (PackageContext.IsPackaged) return TrySetPackaged(enable, out message);
            SetRegistry(enable);
            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }

    // --- Packaged path (WinRT StartupTask) ---

    [SupportedOSPlatform("windows10.0.17763.0")]
    private static bool IsEnabledPackaged()
    {
        try
        {
            var task = Windows.ApplicationModel.StartupTask.GetAsync(TaskId).AsTask().GetAwaiter().GetResult();
            return task.State == Windows.ApplicationModel.StartupTaskState.Enabled
                || task.State == Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
        }
        catch
        {
            return false;
        }
    }

    [SupportedOSPlatform("windows10.0.17763.0")]
    private static bool TrySetPackaged(bool enable, out string? message)
    {
        message = null;
        var task = Windows.ApplicationModel.StartupTask.GetAsync(TaskId).AsTask().GetAwaiter().GetResult();
        if (enable)
        {
            var state = task.RequestEnableAsync().AsTask().GetAwaiter().GetResult();
            switch (state)
            {
                case Windows.ApplicationModel.StartupTaskState.Enabled:
                case Windows.ApplicationModel.StartupTaskState.EnabledByPolicy:
                    return true;
                case Windows.ApplicationModel.StartupTaskState.DisabledByUser:
                    message = "The user disabled this app's autostart in Task Manager → Startup apps. Re-enable it there.";
                    return false;
                case Windows.ApplicationModel.StartupTaskState.DisabledByPolicy:
                    message = "Autostart is blocked by group policy.";
                    return false;
                default:
                    message = $"Autostart state: {state}";
                    return false;
            }
        }
        else
        {
            task.Disable();
            return true;
        }
    }

    // --- Unpackaged path (Registry Run key) ---

    private static bool IsEnabledRegistry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(RunValueName) is not null;
    }

    private static void SetRegistry(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                     ?? Registry.CurrentUser.CreateSubKey(RunKey);
        if (enable)
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exe)) throw new InvalidOperationException("Unable to resolve current executable path.");
            key.SetValue(RunValueName, $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }
}
