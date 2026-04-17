Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$outputRoot = Join-Path $PSScriptRoot "..\Content\Sprites\UI\Myra"
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

function New-Color([int]$r, [int]$g, [int]$b, [int]$a = 255)
{
    return [System.Drawing.Color]::FromArgb($a, $r, $g, $b)
}

function Fill-Gradient(
    [System.Drawing.Graphics]$graphics,
    [int]$x,
    [int]$y,
    [int]$width,
    [int]$height,
    [System.Drawing.Color]$top,
    [System.Drawing.Color]$bottom)
{
    $rect = [System.Drawing.Rectangle]::new($x, $y, $width, $height)
    $brush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        $rect,
        $top,
        $bottom,
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)

    try
    {
        $graphics.FillRectangle($brush, $rect)
    }
    finally
    {
        $brush.Dispose()
    }
}

function Fill-Rect(
    [System.Drawing.Graphics]$graphics,
    [System.Drawing.Color]$color,
    [int]$x,
    [int]$y,
    [int]$width,
    [int]$height)
{
    $brush = [System.Drawing.SolidBrush]::new($color)

    try
    {
        $graphics.FillRectangle($brush, $x, $y, $width, $height)
    }
    finally
    {
        $brush.Dispose()
    }
}

function Draw-Rect(
    [System.Drawing.Graphics]$graphics,
    [System.Drawing.Color]$color,
    [int]$x,
    [int]$y,
    [int]$width,
    [int]$height)
{
    $pen = [System.Drawing.Pen]::new($color)

    try
    {
        $graphics.DrawRectangle($pen, $x, $y, $width, $height)
    }
    finally
    {
        $pen.Dispose()
    }
}

function Draw-HLine(
    [System.Drawing.Graphics]$graphics,
    [System.Drawing.Color]$color,
    [int]$x1,
    [int]$x2,
    [int]$y)
{
    $pen = [System.Drawing.Pen]::new($color)

    try
    {
        $graphics.DrawLine($pen, $x1, $y, $x2, $y)
    }
    finally
    {
        $pen.Dispose()
    }
}

function Draw-VLine(
    [System.Drawing.Graphics]$graphics,
    [System.Drawing.Color]$color,
    [int]$x,
    [int]$y1,
    [int]$y2)
{
    $pen = [System.Drawing.Pen]::new($color)

    try
    {
        $graphics.DrawLine($pen, $x, $y1, $x, $y2)
    }
    finally
    {
        $pen.Dispose()
    }
}

function Draw-Rivets(
    [System.Drawing.Graphics]$graphics,
    [System.Drawing.Color]$light,
    [System.Drawing.Color]$shadow,
    [System.Drawing.Point[]]$points)
{
    foreach ($point in $points)
    {
        Fill-Rect $graphics $shadow $point.X $point.Y 3 3
        Fill-Rect $graphics $light ($point.X + 1) ($point.Y + 1) 1 1
    }
}

function Save-Texture([string]$fileName, [int]$width, [int]$height, [scriptblock]$renderer)
{
    $path = Join-Path $outputRoot $fileName
    $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try
    {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighSpeed

        & $renderer $graphics

        $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "Generated $path"
    }
    finally
    {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$outerShadow = New-Color 8 10 12
$outerLight = New-Color 182 186 188 112
$chromeMidTop = New-Color 98 102 108
$chromeMidBottom = New-Color 44 48 54
$chromeInsetTop = New-Color 72 76 82
$chromeInsetBottom = New-Color 22 24 28
$wellTop = New-Color 21 25 28
$wellBottom = New-Color 10 13 16
$terminalTop = New-Color 18 28 20
$terminalBottom = New-Color 8 13 10
$greenLine = New-Color 118 160 86 104
$greenDim = New-Color 84 118 64 76
$silverDim = New-Color 126 132 136 92
$silverShadow = New-Color 16 18 20 166
$rivetLight = New-Color 160 164 166 84
$rivetShadow = New-Color 12 14 16 184

Save-Texture "ViewportFrame.png" 96 96 {
    param($graphics)

    Fill-Gradient $graphics 0 0 96 96 (New-Color 86 90 96) (New-Color 38 42 46)
    Draw-Rect $graphics $outerShadow 0 0 95 95
    Draw-Rect $graphics $outerLight 1 1 93 93

    Fill-Gradient $graphics 2 2 92 92 (New-Color 26 30 34) (New-Color 16 18 22)
    Draw-Rect $graphics $silverShadow 2 2 91 91
    Draw-Rect $graphics (New-Color 126 132 136 58) 3 3 89 89

    Fill-Gradient $graphics 4 4 88 88 $wellTop $wellBottom
    Draw-Rect $graphics (New-Color 94 126 72 48) 4 4 87 87
    Draw-Rect $graphics (New-Color 118 126 132 24) 5 5 85 85

    Draw-Rivets $graphics $rivetLight $rivetShadow @(
        [System.Drawing.Point]::new(2, 2),
        [System.Drawing.Point]::new(91, 2),
        [System.Drawing.Point]::new(2, 91),
        [System.Drawing.Point]::new(91, 91)
    )
}

Save-Texture "WindowFrame.png" 96 96 {
    param($graphics)

    Fill-Gradient $graphics 0 0 96 96 (New-Color 70 74 80) (New-Color 30 33 38)
    Draw-Rect $graphics $outerShadow 0 0 95 95
    Draw-Rect $graphics $outerLight 1 1 93 93

    Fill-Gradient $graphics 2 2 92 92 (New-Color 22 24 28) (New-Color 12 14 16)
    Draw-Rect $graphics $silverShadow 2 2 91 91
    Draw-Rect $graphics (New-Color 116 122 128 50) 3 3 89 89
    Draw-Rect $graphics (New-Color 98 134 72 32) 4 4 87 87

    Draw-Rivets $graphics $rivetLight $rivetShadow @(
        [System.Drawing.Point]::new(46, 2),
        [System.Drawing.Point]::new(2, 46),
        [System.Drawing.Point]::new(91, 46),
        [System.Drawing.Point]::new(46, 91)
    )
}

Save-Texture "TerminalPanel.png" 96 96 {
    param($graphics)

    Fill-Gradient $graphics 0 0 96 96 (New-Color 14 18 20) (New-Color 10 12 14)
    Draw-Rect $graphics (New-Color 150 154 156 30) 0 0 95 95
    Draw-Rect $graphics (New-Color 10 12 14 150) 1 1 93 93

    Fill-Gradient $graphics 1 1 94 94 (New-Color 15 24 18) (New-Color 8 12 10)
    Draw-Rect $graphics (New-Color 90 122 68 56) 1 1 93 93
    Draw-Rect $graphics (New-Color 88 118 64 22) 2 2 91 91
    Draw-HLine $graphics (New-Color 104 140 76 34) 7 88 5
    Draw-HLine $graphics (New-Color 104 140 76 24) 7 88 90
    Draw-VLine $graphics (New-Color 104 140 76 16) 5 7 88
    Draw-VLine $graphics (New-Color 104 140 76 12) 90 7 88
}

Save-Texture "HeaderPlate.png" 160 64 {
    param($graphics)

    Fill-Gradient $graphics 0 0 160 64 (New-Color 14 18 20) (New-Color 10 12 14)
    Draw-HLine $graphics (New-Color 162 166 170 62) 0 159 0
    Draw-HLine $graphics (New-Color 12 14 16 170) 0 159 63
    Draw-HLine $graphics (New-Color 96 132 72 88) 8 151 8
    Draw-HLine $graphics (New-Color 96 132 72 56) 8 151 9
    Draw-HLine $graphics (New-Color 96 132 72 88) 8 151 54
    Draw-HLine $graphics (New-Color 96 132 72 56) 8 151 55
}
