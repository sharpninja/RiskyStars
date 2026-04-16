#!/usr/bin/env pwsh
# stage-docs.ps1
# Cross-platform PowerShell script to stage documentation for GitBook

param(
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "=== RiskyStars Documentation Staging Script ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean and recreate docs directory structure
Write-Host "[1/3] Cleaning and recreating docs directory structure..." -ForegroundColor Yellow

$docsRoot = Join-Path $PSScriptRoot "docs"

# Remove docs directory if it exists
if (Test-Path $docsRoot) {
    Write-Host "  Removing existing docs directory..." -ForegroundColor Gray
    Remove-Item -Path $docsRoot -Recurse -Force
}

# Create directory structure
Write-Host "  Creating directory structure..." -ForegroundColor Gray
$directories = @(
    "docs",
    "docs/game-concept",
    "docs/rules",
    "docs/server-architecture",
    "docs/client-systems",
    "docs/implementation"
)

foreach ($dir in $directories) {
    $fullPath = Join-Path $PSScriptRoot $dir
    New-Item -ItemType Directory -Path $fullPath -Force | Out-Null
    if ($Verbose) {
        Write-Host "    Created: $dir" -ForegroundColor DarkGray
    }
}

Write-Host "  ✓ Directory structure created" -ForegroundColor Green
Write-Host ""

# Step 2: Copy documentation files
Write-Host "[2/3] Copying documentation files..." -ForegroundColor Yellow

# Define file mappings: source -> destination
$fileMappings = @{
    # Main README
    "docs/README.md" = @{
        Source = $null  # Will be created by hand
        Description = "Main documentation introduction"
    }
    
    # Game Concept section
    "docs/game-concept/README.md" = @{
        Source = $null  # Will be created by hand
        Description = "Game Concept section introduction"
    }
    "docs/game-concept/0.0.0_Game_Concept.md" = @{
        Source = "0.0_Concept/0.0.0_Game_Concept.md"
        Description = "Game concept document"
    }
    
    # Rules section
    "docs/rules/README.md" = @{
        Source = $null  # Will be created by hand
        Description = "Rules section introduction"
    }
    "docs/rules/1.0.00_Gameplay.md" = @{
        Source = "1.0_Rules/1.0.00_Gameplay.md"
        Description = "Gameplay rules"
    }
    "docs/rules/1.1.00_Combat.md" = @{
        Source = "1.0_Rules/1.1.00_Combat.md"
        Description = "Combat rules"
    }
    
    # Server Architecture section
    "docs/server-architecture/README.md" = @{
        Source = $null  # Will be created by hand
        Description = "Server Architecture section introduction"
    }
    "docs/server-architecture/PERSISTENCE.md" = @{
        Source = "RiskyStars.Server/PERSISTENCE.md"
        Description = "Persistence documentation"
    }
    "docs/server-architecture/Maps_README.md" = @{
        Source = "RiskyStars.Server/Maps/README.md"
        Description = "Map systems documentation"
    }
    "docs/server-architecture/README_SessionManager.md" = @{
        Source = "RiskyStars.Server/Services/README_SessionManager.md"
        Description = "Session manager documentation"
    }
    "docs/server-architecture/README_AIPlayerController.md" = @{
        Source = "RiskyStars.Server/Services/README_AIPlayerController.md"
        Description = "AI player controller documentation"
    }
    "docs/server-architecture/README_GameSessionManager_AI_Integration.md" = @{
        Source = "RiskyStars.Server/Services/README_GameSessionManager_AI_Integration.md"
        Description = "AI integration documentation"
    }
    
    # Client Systems section
    "docs/client-systems/README.md" = @{
        Source = $null  # Will be created by hand
        Description = "Client Systems section introduction"
    }
    "docs/client-systems/RENDERING.md" = @{
        Source = "RiskyStars.Client/RENDERING.md"
        Description = "Rendering system documentation"
    }
    "docs/client-systems/INPUT.md" = @{
        Source = "RiskyStars.Client/INPUT.md"
        Description = "Input handling documentation"
    }
    "docs/client-systems/LOBBY.md" = @{
        Source = "RiskyStars.Client/LOBBY.md"
        Description = "Lobby system documentation"
    }
    "docs/client-systems/MAIN_MENU.md" = @{
        Source = "RiskyStars.Client/MAIN_MENU.md"
        Description = "Main menu documentation"
    }
    "docs/client-systems/PLAYER_DASHBOARD.md" = @{
        Source = "RiskyStars.Client/PLAYER_DASHBOARD.md"
        Description = "Player dashboard documentation"
    }
    "docs/client-systems/COMBAT_VISUALIZATION.md" = @{
        Source = "RiskyStars.Client/COMBAT_VISUALIZATION.md"
        Description = "Combat visualization documentation"
    }
    "docs/client-systems/SPRITES.md" = @{
        Source = "RiskyStars.Client/SPRITES.md"
        Description = "Sprite assets documentation"
    }
    "docs/client-systems/AI_VISUALIZATION.md" = @{
        Source = "RiskyStars.Client/AI_VISUALIZATION.md"
        Description = "AI visualization documentation"
    }
    
    # Implementation Summary section
    "docs/implementation/README.md" = @{
        Source = $null  # Will be created by hand
        Description = "Implementation section introduction"
    }
    "docs/implementation/IMPLEMENTATION_SUMMARY.md" = @{
        Source = "IMPLEMENTATION_SUMMARY.md"
        Description = "Implementation summary"
    }
}

$copiedCount = 0
$skippedCount = 0
$errorCount = 0

foreach ($destPath in $fileMappings.Keys | Sort-Object) {
    $mapping = $fileMappings[$destPath]
    $sourcePath = $mapping.Source
    $description = $mapping.Description
    
    if ($null -eq $sourcePath) {
        # These are hand-authored files in the docs directory
        $fullDestPath = Join-Path $PSScriptRoot $destPath
        if (Test-Path $fullDestPath) {
            if ($Verbose) {
                Write-Host "  ✓ $description (already exists)" -ForegroundColor DarkGray
            }
            $skippedCount++
        } else {
            Write-Host "  ⚠ $description (not found, should be hand-authored)" -ForegroundColor DarkYellow
            $errorCount++
        }
        continue
    }
    
    $fullSourcePath = Join-Path $PSScriptRoot $sourcePath
    $fullDestPath = Join-Path $PSScriptRoot $destPath
    
    if (Test-Path $fullSourcePath) {
        Copy-Item -Path $fullSourcePath -Destination $fullDestPath -Force
        if ($Verbose) {
            Write-Host "  ✓ Copied: $sourcePath -> $destPath" -ForegroundColor DarkGray
        }
        $copiedCount++
    } else {
        Write-Host "  ✗ Source not found: $sourcePath" -ForegroundColor Red
        $errorCount++
    }
}

Write-Host "  ✓ Copied $copiedCount files, skipped $skippedCount, $errorCount errors" -ForegroundColor Green
Write-Host ""

# Step 3: Auto-generate SUMMARY.md
Write-Host "[3/3] Auto-generating SUMMARY.md..." -ForegroundColor Yellow

$summaryPath = Join-Path $docsRoot "SUMMARY.md"
$summaryContent = @()

$summaryContent += "# Table of Contents"
$summaryContent += ""
$summaryContent += "* [Introduction](README.md)"
$summaryContent += ""

# Define section structure
$sections = @(
    @{
        Title = "Game Concept"
        Path = "game-concept"
        Description = "Core game ideas, vision, and design philosophy"
    },
    @{
        Title = "Rules"
        Path = "rules"
        Description = "Detailed gameplay mechanics and combat rules"
    },
    @{
        Title = "Server Architecture"
        Path = "server-architecture"
        Description = "Server implementation, persistence, and AI"
    },
    @{
        Title = "Client Systems"
        Path = "client-systems"
        Description = "Client rendering, input, UI, and visualizations"
    },
    @{
        Title = "Implementation Summary"
        Path = "implementation"
        Description = "Implementation status and development progress"
    }
)

foreach ($section in $sections) {
    $summaryContent += "## $($section.Title)"
    $summaryContent += ""
    
    $sectionPath = Join-Path $docsRoot $section.Path
    
    # Add section README
    $sectionReadme = Join-Path $section.Path "README.md"
    $summaryContent += "* [$($section.Title)]($sectionReadme)"
    
    # Get all markdown files in the section (excluding README.md)
    $markdownFiles = Get-ChildItem -Path $sectionPath -Filter "*.md" -File | 
        Where-Object { $_.Name -ne "README.md" } |
        Sort-Object Name
    
    foreach ($file in $markdownFiles) {
        # Extract title from file name or first heading
        $title = $file.BaseName -replace '_', ' ' -replace '^\d+\.\d+\.\d+\s*', ''
        
        # Try to read the first heading from the file
        try {
            $content = Get-Content -Path $file.FullName -First 10
            $heading = $content | Where-Object { $_ -match '^#\s+(.+)$' } | Select-Object -First 1
            if ($heading) {
                if ($heading -match '^#\s+(.+)$') {
                    $title = $Matches[1]
                }
            }
        } catch {
            # Use file name if reading fails
        }
        
        $relativePath = Join-Path $section.Path $file.Name
        $relativePath = $relativePath -replace '\\', '/'
        $summaryContent += "  * [$title]($relativePath)"
    }
    
    $summaryContent += ""
}

# Write SUMMARY.md
$summaryContent | Out-File -FilePath $summaryPath -Encoding UTF8
Write-Host "  ✓ Generated SUMMARY.md with $($sections.Count) sections" -ForegroundColor Green
Write-Host ""

Write-Host "=== Documentation staging complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor White
Write-Host "  • Copied: $copiedCount files" -ForegroundColor Green
Write-Host "  • Skipped: $skippedCount files" -ForegroundColor Yellow
if ($errorCount -gt 0) {
    Write-Host "  • Errors: $errorCount files" -ForegroundColor Red
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Review the generated docs/ directory" -ForegroundColor Gray
Write-Host "  2. Review docs/SUMMARY.md" -ForegroundColor Gray
Write-Host "  3. Commit changes to repository" -ForegroundColor Gray
Write-Host ""
