# Generates Assets\tray.ico and Images\*.png placeholder assets.
# Run with no args; idempotent. No external dependencies.

param(
    [string]$RepoRoot = (Resolve-Path "$PSScriptRoot\..").Path
)

Add-Type -AssemblyName System.Drawing

$assetsDir = Join-Path $RepoRoot 'src\Noty4Me\Assets'
$pkgImg    = Join-Path $RepoRoot 'packaging\Images'
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null
New-Item -ItemType Directory -Force -Path $pkgImg    | Out-Null

# Catppuccin Mocha: Base #1e1e2e, Mauve #cba6f7, Text #cdd6f4
$base   = [System.Drawing.Color]::FromArgb(0xff, 0x1e, 0x1e, 0x2e)
$mauve  = [System.Drawing.Color]::FromArgb(0xff, 0xcb, 0xa6, 0xf7)
$text   = [System.Drawing.Color]::FromArgb(0xff, 0xcd, 0xd6, 0xf4)

function New-NotyPng([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    $r = [int]([math]::Floor($size * 0.18))
    $rect = New-Object System.Drawing.Rectangle 0, 0, $size, $size
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90) | Out-Null
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90) | Out-Null
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90) | Out-Null
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90) | Out-Null
    $path.CloseFigure()

    $bg = New-Object System.Drawing.SolidBrush $base
    $g.FillPath($bg, $path)
    $bg.Dispose()

    $accent = New-Object System.Drawing.SolidBrush $mauve
    $barH = [int]([math]::Round($size * 0.10))
    $marginX = [int]([math]::Round($size * 0.22))
    $startY = [int]([math]::Round($size * 0.28))
    $gap = [int]([math]::Round($size * 0.10))
    for ($i = 0; $i -lt 3; $i++) {
        $w = $size - 2 * $marginX
        if ($i -eq 2) { $w = [int]($w * 0.6) }
        $y = $startY + $i * ($barH + $gap)
        $br = if ($i -eq 0) { $accent } else { (New-Object System.Drawing.SolidBrush $text) }
        $g.FillRectangle($br, $marginX, $y, $w, $barH)
        if ($br -ne $accent) { $br.Dispose() }
    }
    $accent.Dispose()
    $g.Dispose()
    return $bmp
}

# 1) Build the .ico with multiple sizes
$icoPath = Join-Path $assetsDir 'tray.ico'
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$bitmaps = @{}
foreach ($s in $sizes) { $bitmaps[$s] = New-NotyPng -size $s }

# ICO file format: header (6) + entries (16 each) + image data (PNG)
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $ms
$bw.Write([uint16]0)        # reserved
$bw.Write([uint16]1)        # type 1 = icon
$bw.Write([uint16]$sizes.Count)

$dataOffset = 6 + 16 * $sizes.Count
$pngBlobs = @()
foreach ($s in $sizes) {
    $pms = New-Object System.IO.MemoryStream
    $bitmaps[$s].Save($pms, [System.Drawing.Imaging.ImageFormat]::Png)
    $blob = $pms.ToArray()
    $pms.Dispose()
    $pngBlobs += ,@($s, $blob, $dataOffset)
    $dataOffset += $blob.Length
}

foreach ($e in $pngBlobs) {
    $size = $e[0]; $blob = $e[1]; $offset = $e[2]
    $bw.Write([byte]([math]::Min($size, 255) % 256))  # width (0 = 256)
    $bw.Write([byte]([math]::Min($size, 255) % 256))  # height
    $bw.Write([byte]0)            # palette
    $bw.Write([byte]0)            # reserved
    $bw.Write([uint16]1)          # planes
    $bw.Write([uint16]32)         # bpp
    $bw.Write([uint32]$blob.Length)
    $bw.Write([uint32]$offset)
}
foreach ($e in $pngBlobs) { $bw.Write($e[1]) }

$bw.Flush()
[System.IO.File]::WriteAllBytes($icoPath, $ms.ToArray())
$bw.Dispose(); $ms.Dispose()
Write-Host "Wrote $icoPath"

# 2) MSIX visual assets
function Save-Png([int]$w, [int]$h, [string]$path) {
    $b = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($b)
    $g.Clear($base)
    $glyph = New-NotyPng -size ([math]::Min($w, $h))
    $gx = [int](($w - $glyph.Width) / 2)
    $gy = [int](($h - $glyph.Height) / 2)
    $g.DrawImage($glyph, $gx, $gy)
    $glyph.Dispose()
    $g.Dispose()
    $b.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $b.Dispose()
}

Save-Png 44   44   (Join-Path $pkgImg 'Square44x44Logo.png')
Save-Png 150  150  (Join-Path $pkgImg 'Square150x150Logo.png')
Save-Png 50   50   (Join-Path $pkgImg 'StoreLogo.png')
Save-Png 620  300  (Join-Path $pkgImg 'Wide310x150Logo.png')
Save-Png 71   71   (Join-Path $pkgImg 'Square71x71Logo.png')
Save-Png 310  310  (Join-Path $pkgImg 'Square310x310Logo.png')

foreach ($s in $sizes) { $bitmaps[$s].Dispose() }
Write-Host "Wrote MSIX assets to $pkgImg"
