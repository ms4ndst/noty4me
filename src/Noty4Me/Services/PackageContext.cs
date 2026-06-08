using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Noty4Me.Services;

// Detects whether this process runs with a packaged identity (MSIX) or
// as a plain unpackaged Win32 process. Cached after first call.
public static class PackageContext
{
    private const int APPMODEL_ERROR_NO_PACKAGE = 15700;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint length, StringBuilder? fullName);

    private static bool? _isPackaged;

    public static bool IsPackaged
    {
        get
        {
            if (_isPackaged is bool v) return v;
            try
            {
                uint len = 0;
                var rc = GetCurrentPackageFullName(ref len, null);
                _isPackaged = rc != APPMODEL_ERROR_NO_PACKAGE;
            }
            catch
            {
                _isPackaged = false;
            }
            return _isPackaged.Value;
        }
    }
}
