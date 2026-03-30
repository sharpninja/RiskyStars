@echo off
echo Initializing sprite placeholders for RiskyStars...
echo.

cd /d "%~dp0..\..\Tools"

if not exist "CreatePlaceholders.csproj" (
    echo Error: CreatePlaceholders.csproj not found
    echo Please ensure the Tools directory exists with the CreatePlaceholders project.
    pause
    exit /b 1
)

echo Running CreatePlaceholders tool...
dotnet run --project CreatePlaceholders.csproj

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Sprite placeholders created successfully!
    echo.
    echo All sprite assets are now ready for use in the Content Pipeline.
    echo Run 'dotnet build RiskyStars.Client' to compile the content.
) else (
    echo.
    echo Error running CreatePlaceholders tool
    pause
    exit /b 1
)

echo.
pause
