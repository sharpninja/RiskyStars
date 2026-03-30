# Initialize Sprite Placeholders
# This script generates placeholder PNG files for all game sprites

Write-Host "Initializing sprite placeholders for RiskyStars..." -ForegroundColor Cyan
Write-Host ""

$toolPath = Join-Path $PSScriptRoot ".." ".." "Tools" "CreatePlaceholders.csproj"

if (Test-Path $toolPath) {
    Write-Host "Running CreatePlaceholders tool..." -ForegroundColor Yellow
    Push-Location (Join-Path $PSScriptRoot ".." ".." "Tools")
    
    try {
        dotnet run --project CreatePlaceholders.csproj
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "Sprite placeholders created successfully!" -ForegroundColor Green
        } else {
            Write-Host ""
            Write-Host "Error running CreatePlaceholders tool" -ForegroundColor Red
            exit 1
        }
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "Error: CreatePlaceholders.csproj not found at $toolPath" -ForegroundColor Red
    Write-Host "Please ensure the Tools directory exists with the CreatePlaceholders project." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "All sprite assets are now ready for use in the Content Pipeline." -ForegroundColor Green
Write-Host "Run 'dotnet build RiskyStars.Client' to compile the content." -ForegroundColor Cyan
