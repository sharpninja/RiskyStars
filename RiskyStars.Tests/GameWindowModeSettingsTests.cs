using System.Text.Json;
using RiskyStars.Client;

namespace RiskyStars.Tests;

[CollectionDefinition("Settings file tests", DisableParallelization = true)]
public class SettingsFileTestCollection
{
}

[Collection("Settings file tests")]
public class GameWindowModeSettingsTests
{
    [Theory]
    [InlineData("Normal", GameWindowMode.Normal)]
    [InlineData("Windowed", GameWindowMode.Normal)]
    [InlineData("Maximized", GameWindowMode.Maximized)]
    [InlineData("Full", GameWindowMode.Full)]
    [InlineData("Fullscreen", GameWindowMode.Full)]
    public void ParseWindowModeOption_MapsSupportedAndLegacyLabels(string option, GameWindowMode expectedMode)
    {
        Assert.Equal(expectedMode, Settings.ParseWindowModeOption(option));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Borderless")]
    public void ParseWindowModeOption_FallsBackToNormalForUnsupportedLabels(string? option)
    {
        Assert.Equal(GameWindowMode.Normal, Settings.ParseWindowModeOption(option));
    }

    [Theory]
    [InlineData(GameWindowMode.Normal, "Normal", 0)]
    [InlineData(GameWindowMode.Maximized, "Maximized", 1)]
    [InlineData(GameWindowMode.Full, "Full", 2)]
    public void GetWindowModeOption_ReturnsStableUiLabels(GameWindowMode mode, string expectedOption, int expectedIndex)
    {
        Assert.Equal(expectedOption, Settings.GetWindowModeOption(mode));
        Assert.Equal(expectedIndex, Settings.GetWindowModeOptionIndex(mode));
    }

    [Fact]
    public void Normalize_UpgradesLegacyFullscreenFlagToFullMode()
    {
        var settings = JsonSerializer.Deserialize<Settings>("""
        {
          "ResolutionWidth": 1280,
          "ResolutionHeight": 720,
          "Fullscreen": true
        }
        """)!;

        settings.Normalize();

        Assert.Equal(GameWindowMode.Full, settings.WindowMode);
        Assert.True(settings.Fullscreen);
    }

    [Fact]
    public void Normalize_PreservesMaximizedModeAndClearsLegacyFullscreenFlag()
    {
        var settings = new Settings
        {
            WindowMode = GameWindowMode.Maximized,
            Fullscreen = true
        };

        settings.Normalize();

        Assert.Equal(GameWindowMode.Maximized, settings.WindowMode);
        Assert.False(settings.Fullscreen);
    }

    [Fact]
    public void Normalize_FallsBackFromInvalidModeWithoutKeepingBadValue()
    {
        var settings = new Settings
        {
            WindowMode = (GameWindowMode)999,
            Fullscreen = false
        };

        settings.Normalize();

        Assert.Equal(GameWindowMode.Normal, settings.WindowMode);
        Assert.False(settings.Fullscreen);
    }

    [Fact]
    public void Clone_PreservesWindowMode()
    {
        var settings = new Settings
        {
            WindowMode = GameWindowMode.Maximized
        };
        settings.Normalize();

        var clone = settings.Clone();

        Assert.Equal(GameWindowMode.Maximized, clone.WindowMode);
        Assert.False(clone.Fullscreen);
    }

    [Theory]
    [InlineData(GameWindowMode.Normal, false, false, true)]
    [InlineData(GameWindowMode.Maximized, false, true, false)]
    [InlineData(GameWindowMode.Full, true, false, false)]
    public void CreatePlan_MapsModeToGraphicsAndDesktopActions(
        GameWindowMode mode,
        bool expectedFullscreen,
        bool expectedMaximize,
        bool expectedRestore)
    {
        var plan = GameWindowModeController.CreatePlan(mode);

        Assert.Equal(expectedFullscreen, plan.IsFullscreen);
        Assert.Equal(expectedMaximize, plan.MaximizeWindow);
        Assert.Equal(expectedRestore, plan.RestoreWindow);
    }

    [Theory]
    [InlineData(true, true, GameWindowMode.Full)]
    [InlineData(true, false, GameWindowMode.Full)]
    [InlineData(false, true, GameWindowMode.Maximized)]
    [InlineData(false, false, GameWindowMode.Normal)]
    public void DetectMode_PrioritizesFullscreenThenMaximized(bool isFullscreen, bool isMaximized, GameWindowMode expectedMode)
    {
        Assert.Equal(expectedMode, GameWindowModeController.DetectMode(isFullscreen, isMaximized));
    }

    [Theory]
    [InlineData(GameWindowMode.Normal, 1280, 720, false)]
    [InlineData(GameWindowMode.Normal, 1600, 900, true)]
    [InlineData(GameWindowMode.Maximized, 1600, 900, false)]
    [InlineData(GameWindowMode.Full, 1280, 720, true)]
    public void HasDisplaySettingsChanged_IgnoresResolutionDifferencesOnlyWhenMaximized(
        GameWindowMode requestedMode,
        int requestedWidth,
        int requestedHeight,
        bool expectedChanged)
    {
        var requested = new Settings
        {
            ResolutionWidth = requestedWidth,
            ResolutionHeight = requestedHeight,
            WindowMode = requestedMode
        };
        requested.Normalize();

        var applied = new AppliedWindowDisplayState(
            BackBufferWidth: 1280,
            BackBufferHeight: 720,
            IsFullscreen: false,
            WindowMode: requestedMode);

        Assert.Equal(expectedChanged, WindowDisplaySettings.HasDisplaySettingsChanged(requested, applied));
    }

    [Fact]
    public void HasDisplaySettingsChanged_DetectsWindowModeMismatch()
    {
        var requested = new Settings
        {
            WindowMode = GameWindowMode.Maximized
        };
        requested.Normalize();

        var applied = new AppliedWindowDisplayState(1280, 720, false, GameWindowMode.Normal);

        Assert.True(WindowDisplaySettings.HasDisplaySettingsChanged(requested, applied));
    }

    [Fact]
    public void CaptureCurrentDisplay_CapturesFullscreenModeAndResolution()
    {
        var settings = new Settings
        {
            ResolutionWidth = 1280,
            ResolutionHeight = 720,
            WindowMode = GameWindowMode.Normal
        };

        WindowDisplaySettings.CaptureCurrentDisplay(
            settings,
            new AppliedWindowDisplayState(2560, 1440, true, GameWindowMode.Full));

        Assert.Equal(GameWindowMode.Full, settings.WindowMode);
        Assert.True(settings.Fullscreen);
        Assert.Equal(2560, settings.ResolutionWidth);
        Assert.Equal(1440, settings.ResolutionHeight);
    }

    [Fact]
    public void CaptureCurrentDisplay_DoesNotKeepOldResolutionWhenFullscreenSizeChanged()
    {
        var settings = new Settings
        {
            ResolutionWidth = 1280,
            ResolutionHeight = 720,
            WindowMode = GameWindowMode.Full
        };

        WindowDisplaySettings.CaptureCurrentDisplay(
            settings,
            new AppliedWindowDisplayState(1920, 1080, true, GameWindowMode.Full));

        Assert.NotEqual(1280, settings.ResolutionWidth);
        Assert.NotEqual(720, settings.ResolutionHeight);
        Assert.Equal(1920, settings.ResolutionWidth);
        Assert.Equal(1080, settings.ResolutionHeight);
    }

    [Fact]
    public void CaptureCurrentDisplay_CapturesNormalModeAndResolution()
    {
        var settings = new Settings
        {
            ResolutionWidth = 1280,
            ResolutionHeight = 720,
            WindowMode = GameWindowMode.Maximized
        };

        WindowDisplaySettings.CaptureCurrentDisplay(
            settings,
            new AppliedWindowDisplayState(1600, 900, false, GameWindowMode.Normal));

        Assert.Equal(GameWindowMode.Normal, settings.WindowMode);
        Assert.False(settings.Fullscreen);
        Assert.Equal(1600, settings.ResolutionWidth);
        Assert.Equal(900, settings.ResolutionHeight);
    }

    [Fact]
    public void CaptureCurrentDisplay_PreservesNormalResolutionWhenMaximized()
    {
        var settings = new Settings
        {
            ResolutionWidth = 1280,
            ResolutionHeight = 720,
            WindowMode = GameWindowMode.Normal
        };

        WindowDisplaySettings.CaptureCurrentDisplay(
            settings,
            new AppliedWindowDisplayState(2560, 1369, false, GameWindowMode.Maximized));

        Assert.Equal(GameWindowMode.Maximized, settings.WindowMode);
        Assert.False(settings.Fullscreen);
        Assert.Equal(1280, settings.ResolutionWidth);
        Assert.Equal(720, settings.ResolutionHeight);
    }

    [Fact]
    public void CaptureCurrentDisplay_DoesNotOverwriteResolutionWithInvalidBounds()
    {
        var settings = new Settings
        {
            ResolutionWidth = 1600,
            ResolutionHeight = 900,
            WindowMode = GameWindowMode.Normal
        };

        WindowDisplaySettings.CaptureCurrentDisplay(
            settings,
            new AppliedWindowDisplayState(0, 1080, false, GameWindowMode.Normal));

        Assert.Equal(GameWindowMode.Normal, settings.WindowMode);
        Assert.Equal(1600, settings.ResolutionWidth);
        Assert.Equal(900, settings.ResolutionHeight);
    }

    [Theory]
    [InlineData(GameWindowMode.Normal, 1280, 720, true)]
    [InlineData(GameWindowMode.Maximized, 1280, 720, false)]
    [InlineData(GameWindowMode.Full, 1280, 720, true)]
    [InlineData(GameWindowMode.Full, 0, 720, false)]
    [InlineData(GameWindowMode.Full, 1280, 0, false)]
    public void ShouldCaptureResolution_RequiresValidSizeAndSkipsMaximized(
        GameWindowMode mode,
        int width,
        int height,
        bool expectedCapture)
    {
        Assert.Equal(expectedCapture, WindowDisplaySettings.ShouldCaptureResolution(mode, width, height));
    }

    [Fact]
    public void GetResolutionOptions_ReturnsSupportedResolutionsAndKeepsCustomCurrentResolution()
    {
        var defaultOptions = Settings.GetResolutionOptions();
        var customOptions = Settings.GetResolutionOptions("1024x768");

        Assert.Contains("1280x720", defaultOptions);
        Assert.Equal("1024x768", customOptions[0]);
    }

    [Fact]
    public void Normalize_RepairsInvalidScalarAndThemeSettings()
    {
        var settings = new Settings
        {
            ServerAddress = " ",
            ResolutionWidth = 1,
            ResolutionHeight = 1,
            UiScalePercent = -25,
            MapCamera = null!,
            Theme = new UiThemeSettings
            {
                AccentColor = "Bad Accent",
                WarningColor = "Bad Warning",
                FontStyle = "Bad Font",
                FontScalePercent = 1,
                PaddingScalePercent = 999,
                FramePaddingPercent = 1,
                ContrastPercent = 999
            }
        };

        settings.Normalize();

        Assert.Equal("http://localhost:5000", settings.ServerAddress);
        Assert.Equal(800, settings.ResolutionWidth);
        Assert.Equal(600, settings.ResolutionHeight);
        Assert.Equal(100, settings.UiScalePercent);
        Assert.Equal("Classic Green", settings.Theme.AccentColor);
        Assert.Equal("Amber", settings.Theme.WarningColor);
        Assert.Equal("Standard", settings.Theme.FontStyle);
        Assert.Equal(80, settings.Theme.FontScalePercent);
        Assert.Equal(150, settings.Theme.PaddingScalePercent);
        Assert.Equal(70, settings.Theme.FramePaddingPercent);
        Assert.Equal(140, settings.Theme.ContrastPercent);
        Assert.NotNull(settings.MapCamera);
        Assert.False(settings.MapCamera.HasSavedView);
    }

    [Fact]
    public void SaveAndLoad_PersistsWindowModeAndDisplaySettings()
    {
        RunInTemporarySettingsDirectory(() =>
        {
            var settings = new Settings
            {
                ServerAddress = "http://example.test:5001",
                ResolutionWidth = 1600,
                ResolutionHeight = 900,
                UiScalePercent = 125,
                WindowMode = GameWindowMode.Maximized,
                VSync = false,
                TargetFrameRate = 120,
                MasterVolume = 0.5f,
                MusicVolume = 0.25f,
                SfxVolume = 0.75f,
                CameraPanSpeed = 7,
                CameraZoomSpeed = 0.2f,
                InvertCameraZoom = true,
                ShowDebugInfo = true,
                ShowFPS = false,
                MapCamera = new MapCameraSettings
                {
                    HasSavedView = true,
                    PositionX = 245.5f,
                    PositionY = -130.25f,
                    Zoom = 2.5f
                },
                Theme = new UiThemeSettings
                {
                    AccentColor = "Ice Cyan",
                    WarningColor = "Cyan",
                    FontStyle = "Command",
                    FontScalePercent = 110,
                    PaddingScalePercent = 120,
                    FramePaddingPercent = 115,
                    ContrastPercent = 125
                }
            };

            settings.Save();
            var loaded = Settings.Load();

            Assert.Equal("http://example.test:5001", loaded.ServerAddress);
            Assert.Equal(1600, loaded.ResolutionWidth);
            Assert.Equal(900, loaded.ResolutionHeight);
            Assert.Equal(125, loaded.UiScalePercent);
            Assert.Equal(GameWindowMode.Maximized, loaded.WindowMode);
            Assert.False(loaded.Fullscreen);
            Assert.False(loaded.VSync);
            Assert.Equal(120, loaded.TargetFrameRate);
            Assert.Equal(0.5f, loaded.MasterVolume);
            Assert.Equal(0.25f, loaded.MusicVolume);
            Assert.Equal(0.75f, loaded.SfxVolume);
            Assert.Equal(7, loaded.CameraPanSpeed);
            Assert.Equal(0.2f, loaded.CameraZoomSpeed);
            Assert.True(loaded.InvertCameraZoom);
            Assert.True(loaded.ShowDebugInfo);
            Assert.False(loaded.ShowFPS);
            Assert.True(loaded.MapCamera.HasSavedView);
            Assert.Equal(245.5f, loaded.MapCamera.PositionX);
            Assert.Equal(-130.25f, loaded.MapCamera.PositionY);
            Assert.Equal(2.5f, loaded.MapCamera.Zoom);
            Assert.Equal("Ice Cyan", loaded.Theme.AccentColor);
            Assert.Equal("Cyan", loaded.Theme.WarningColor);
            Assert.Equal("Command", loaded.Theme.FontStyle);
            Assert.Equal(110, loaded.Theme.FontScalePercent);
            Assert.Equal(120, loaded.Theme.PaddingScalePercent);
            Assert.Equal(115, loaded.Theme.FramePaddingPercent);
            Assert.Equal(125, loaded.Theme.ContrastPercent);
        });
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenSettingsFileIsMissingOrInvalid()
    {
        RunInTemporarySettingsDirectory(() =>
        {
            var missingFileSettings = Settings.Load();
            Assert.Equal(GameWindowMode.Normal, missingFileSettings.WindowMode);
            Assert.False(missingFileSettings.MapCamera.HasSavedView);

            File.WriteAllText("settings.json", "{ invalid json");
            var invalidFileSettings = Settings.Load();

            Assert.Equal("http://localhost:5000", invalidFileSettings.ServerAddress);
            Assert.Equal(GameWindowMode.Normal, invalidFileSettings.WindowMode);
            Assert.False(invalidFileSettings.MapCamera.HasSavedView);
        });
    }

    [Fact]
    public void SettingsClone_CopiesAllPersistedSettingsWithoutSharingThemeReference()
    {
        var settings = new Settings
        {
            ServerAddress = "http://clone.test",
            ResolutionWidth = 1920,
            ResolutionHeight = 1080,
            UiScalePercent = 130,
            WindowMode = GameWindowMode.Full,
            VSync = false,
            TargetFrameRate = 144,
            MasterVolume = 0.4f,
            MusicVolume = 0.3f,
            SfxVolume = 0.2f,
            CameraPanSpeed = 8,
            CameraZoomSpeed = 0.3f,
            InvertCameraZoom = true,
            ShowDebugInfo = true,
            ShowFPS = false,
            MapCamera = new MapCameraSettings
            {
                HasSavedView = true,
                PositionX = 512f,
                PositionY = -256f,
                Zoom = 3f
            },
            Theme = new UiThemeSettings
            {
                AccentColor = "Amber Gold",
                WarningColor = "Ivory",
                FontStyle = "Compact",
                FontScalePercent = 105,
                PaddingScalePercent = 115,
                FramePaddingPercent = 125,
                ContrastPercent = 135
            }
        };
        settings.Normalize();

        var clone = settings.Clone();

        Assert.NotSame(settings.Theme, clone.Theme);
        Assert.NotSame(settings.MapCamera, clone.MapCamera);
        Assert.Equal(settings.ServerAddress, clone.ServerAddress);
        Assert.Equal(settings.ResolutionWidth, clone.ResolutionWidth);
        Assert.Equal(settings.ResolutionHeight, clone.ResolutionHeight);
        Assert.Equal(settings.UiScalePercent, clone.UiScalePercent);
        Assert.Equal(settings.WindowMode, clone.WindowMode);
        Assert.Equal(settings.Fullscreen, clone.Fullscreen);
        Assert.Equal(settings.VSync, clone.VSync);
        Assert.Equal(settings.TargetFrameRate, clone.TargetFrameRate);
        Assert.Equal(settings.MasterVolume, clone.MasterVolume);
        Assert.Equal(settings.MusicVolume, clone.MusicVolume);
        Assert.Equal(settings.SfxVolume, clone.SfxVolume);
        Assert.Equal(settings.CameraPanSpeed, clone.CameraPanSpeed);
        Assert.Equal(settings.CameraZoomSpeed, clone.CameraZoomSpeed);
        Assert.Equal(settings.InvertCameraZoom, clone.InvertCameraZoom);
        Assert.Equal(settings.ShowDebugInfo, clone.ShowDebugInfo);
        Assert.Equal(settings.ShowFPS, clone.ShowFPS);
        Assert.Equal(settings.MapCamera.HasSavedView, clone.MapCamera.HasSavedView);
        Assert.Equal(settings.MapCamera.PositionX, clone.MapCamera.PositionX);
        Assert.Equal(settings.MapCamera.PositionY, clone.MapCamera.PositionY);
        Assert.Equal(settings.MapCamera.Zoom, clone.MapCamera.Zoom);
        Assert.Equal(settings.Theme.AccentColor, clone.Theme.AccentColor);
        Assert.Equal(settings.Theme.WarningColor, clone.Theme.WarningColor);
        Assert.Equal(settings.Theme.FontStyle, clone.Theme.FontStyle);
        Assert.Equal(settings.Theme.FontScalePercent, clone.Theme.FontScalePercent);
        Assert.Equal(settings.Theme.PaddingScalePercent, clone.Theme.PaddingScalePercent);
        Assert.Equal(settings.Theme.FramePaddingPercent, clone.Theme.FramePaddingPercent);
        Assert.Equal(settings.Theme.ContrastPercent, clone.Theme.ContrastPercent);
    }

    private static void RunInTemporarySettingsDirectory(Action action)
    {
        var originalDirectory = Environment.CurrentDirectory;
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"risky-stars-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            Environment.CurrentDirectory = tempDirectory;
            action();
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
