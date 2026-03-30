# PowerShell script to generate placeholder sprite images
# This script creates simple colored PNG images as placeholders

$contentPath = Join-Path $PSScriptRoot ".." "Content" "Sprites"

# Ensure directories exist
$dirs = @(
    "StellarBodies",
    "Armies",
    "UI",
    "HyperspaceLanes",
    "Combat"
)

foreach ($dir in $dirs) {
    $fullPath = Join-Path $contentPath $dir
    if (!(Test-Path $fullPath)) {
        New-Item -ItemType Directory -Path $fullPath -Force | Out-Null
    }
}

Write-Host "Generating sprite placeholders..." -ForegroundColor Cyan

# Load System.Drawing
Add-Type -AssemblyName System.Drawing

function New-ColoredCircle {
    param(
        [string]$Path,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Color]$Color
    )
    
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)
    
    $brush = New-Object System.Drawing.SolidBrush($Color)
    $graphics.FillEllipse($brush, 2, 2, $Width - 4, $Height - 4)
    
    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    $graphics.Dispose()
    $brush.Dispose()
    
    Write-Host "  Created: $(Split-Path $Path -Leaf)" -ForegroundColor Green
}

function New-ColoredRectangle {
    param(
        [string]$Path,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Color]$Color
    )
    
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)
    
    $brush = New-Object System.Drawing.SolidBrush($Color)
    $graphics.FillRectangle($brush, 2, 2, $Width - 4, $Height - 4)
    
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(200, $Color.R, $Color.G, $Color.B), 2)
    $graphics.DrawRectangle($pen, 2, 2, $Width - 4, $Height - 4)
    
    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    $graphics.Dispose()
    $brush.Dispose()
    $pen.Dispose()
    
    Write-Host "  Created: $(Split-Path $Path -Leaf)" -ForegroundColor Green
}

# Generate Stellar Bodies
Write-Host "`nGenerating Stellar Body sprites..." -ForegroundColor Yellow
$dir = Join-Path $contentPath "StellarBodies"
New-ColoredCircle -Path (Join-Path $dir "GasGiant.png") -Width 64 -Height 64 -Color ([System.Drawing.Color]::FromArgb(200, 150, 100))
New-ColoredCircle -Path (Join-Path $dir "GasGiant_Variant1.png") -Width 64 -Height 64 -Color ([System.Drawing.Color]::FromArgb(220, 180, 100))
New-ColoredCircle -Path (Join-Path $dir "GasGiant_Variant2.png") -Width 64 -Height 64 -Color ([System.Drawing.Color]::FromArgb(220, 120, 90))
New-ColoredCircle -Path (Join-Path $dir "RockyPlanet.png") -Width 48 -Height 48 -Color ([System.Drawing.Color]::FromArgb(100, 150, 200))
New-ColoredCircle -Path (Join-Path $dir "RockyPlanet_Variant1.png") -Width 48 -Height 48 -Color ([System.Drawing.Color]::FromArgb(200, 160, 100))
New-ColoredCircle -Path (Join-Path $dir "RockyPlanet_Variant2.png") -Width 48 -Height 48 -Color ([System.Drawing.Color]::FromArgb(180, 200, 230))
New-ColoredCircle -Path (Join-Path $dir "Planetoid.png") -Width 24 -Height 24 -Color ([System.Drawing.Color]::FromArgb(150, 150, 150))
New-ColoredCircle -Path (Join-Path $dir "Comet.png") -Width 32 -Height 32 -Color ([System.Drawing.Color]::FromArgb(150, 200, 255))

# Generate Armies
Write-Host "`nGenerating Army sprites..." -ForegroundColor Yellow
$dir = Join-Path $contentPath "Armies"
New-ColoredRectangle -Path (Join-Path $dir "Army.png") -Width 32 -Height 32 -Color ([System.Drawing.Color]::FromArgb(180, 180, 180))
New-ColoredRectangle -Path (Join-Path $dir "Hero.png") -Width 32 -Height 32 -Color ([System.Drawing.Color]::FromArgb(220, 180, 60))

# Generate UI
Write-Host "`nGenerating UI sprites..." -ForegroundColor Yellow
$dir = Join-Path $contentPath "UI"
New-ColoredRectangle -Path (Join-Path $dir "ButtonNormal.png") -Width 120 -Height 40 -Color ([System.Drawing.Color]::FromArgb(80, 80, 100))
New-ColoredRectangle -Path (Join-Path $dir "ButtonHover.png") -Width 120 -Height 40 -Color ([System.Drawing.Color]::FromArgb(100, 100, 120))
New-ColoredRectangle -Path (Join-Path $dir "ButtonPressed.png") -Width 120 -Height 40 -Color ([System.Drawing.Color]::FromArgb(60, 60, 80))
New-ColoredRectangle -Path (Join-Path $dir "Panel.png") -Width 200 -Height 150 -Color ([System.Drawing.Color]::FromArgb(200, 40, 40, 50))
New-ColoredRectangle -Path (Join-Path $dir "IconProduction.png") -Width 32 -Height 32 -Color ([System.Drawing.Color]::FromArgb(180, 140, 60))
New-ColoredRectangle -Path (Join-Path $dir "IconEnergy.png") -Width 32 -Height 32 -Color ([System.Drawing.Color]::FromArgb(255, 220, 60))

# Generate Hyperspace Lanes
Write-Host "`nGenerating Hyperspace Lane sprites..." -ForegroundColor Yellow
$dir = Join-Path $contentPath "HyperspaceLanes"
New-ColoredRectangle -Path (Join-Path $dir "Lane.png") -Width 32 -Height 8 -Color ([System.Drawing.Color]::FromArgb(150, 150, 150))
New-ColoredCircle -Path (Join-Path $dir "LaneMouth.png") -Width 32 -Height 32 -Color ([System.Drawing.Color]::FromArgb(100, 150, 200))

# Generate Combat
Write-Host "`nGenerating Combat sprites..." -ForegroundColor Yellow
$dir = Join-Path $contentPath "Combat"
New-ColoredCircle -Path (Join-Path $dir "Hit.png") -Width 32 -Height 32 -Color ([System.Drawing.Color]::FromArgb(255, 200, 100))
New-ColoredCircle -Path (Join-Path $dir "Miss.png") -Width 32 -Height 32 -Color ([System.Drawing.Color]::FromArgb(150, 150, 180))
New-ColoredCircle -Path (Join-Path $dir "Explosion.png") -Width 48 -Height 48 -Color ([System.Drawing.Color]::FromArgb(255, 100, 50))
New-ColoredRectangle -Path (Join-Path $dir "DiceRoll.png") -Width 48 -Height 48 -Color ([System.Drawing.Color]::FromArgb(240, 240, 240))

Write-Host "`nAll placeholders generated successfully!" -ForegroundColor Cyan
