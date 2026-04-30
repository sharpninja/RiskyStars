using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;
using RiskyStars.Shared;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RiskyStars.Tests;

[Collection("Client debug screenshot tests")]
public sealed class ClientDebugGameScreenshotIntegrationTests
{
    private const int ScreenshotWidth = 1536;
    private const int ScreenshotHeight = 832;

    private static readonly string[] ScreenIds =
    [
        "main-menu",
        "main-menu-settings",
        "main-menu-connecting",
        "game-mode-selector",
        "connection-screen",
        "lobby-browser",
        "create-lobby",
        "multiplayer-lobby",
        "single-player-lobby",
        "gameplay-hud-top-bar",
        "gameplay-hud-legend",
        "side-panel-container",
        "settings-window",
        "debug-info-window",
        "player-dashboard-window",
        "ai-visualization-window",
        "encyclopedia-window",
        "ui-scale-window",
        "tutorial-mode-window",
        "continent-zoom-window",
        "combat-hud-overlay",
        "server-status-indicator",
        "dialog-manager",
        "combat-event-dialog",
        "context-menu-manager",
        "combat-screen",
        "legacy-player-dashboard",
        "ai-action-indicator"
    ];

    static ClientDebugGameScreenshotIntegrationTests()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    [Fact]
    public async Task ValidationSequence_NavigatesEveryDocumentedScreenAndCapturesHwndScreenshots()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string repositoryRoot = FindRepositoryRoot();
        string clientOutputDirectory = Path.Combine(repositoryRoot, "RiskyStars.Client", "bin", "Debug", "net9.0");
        string clientExecutable = Path.Combine(clientOutputDirectory, "RiskyStars.Client.exe");
        Assert.True(File.Exists(clientExecutable), $"Build the client before running screenshot validation. Missing: {clientExecutable}");

        string screenshotDirectory = Path.Combine(repositoryRoot, "RiskyStars.Client", "Screenshots", "Actual");
        Directory.CreateDirectory(screenshotDirectory);

        int debugPort = GetAvailableTcpPort();
        string settingsPath = Path.Combine(clientOutputDirectory, "settings.json");
        string preferencesPath = Path.Combine(clientOutputDirectory, "window_preferences.json");
        string? originalSettings = ReadExistingFile(settingsPath);
        string? originalPreferences = ReadExistingFile(preferencesPath);

        Process? process = null;
        try
        {
            WriteDeterministicClientSettings(settingsPath);
            File.WriteAllText(preferencesPath, "{}");

            process = StartClient(clientExecutable, clientOutputDirectory, debugPort);
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            IntPtr hwnd = await WaitForMainWindowHandleAsync(process, TimeSpan.FromSeconds(30));
            MoveClientAreaToMonitorWorkArea(hwnd);

            using var channel = GrpcChannel.ForAddress(
                $"http://localhost:{debugPort.ToString(CultureInfo.InvariantCulture)}",
                new GrpcChannelOptions
                {
                    HttpHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true
                    }
                });
            var client = new ClientDebugProtocol.ClientDebugProtocolClient(channel);
            await WaitForDebugProtocolAsync(client, TimeSpan.FromSeconds(30));

            foreach (string screenId in ScreenIds)
            {
                ClientDebugActionResponse navigate = await client.NavigateToScreenAsync(
                    new NavigateToScreenRequest { ScreenId = screenId });
                Assert.True(navigate.Success, $"{screenId}: {navigate.Message}");

                await Task.Delay(TimeSpan.FromMilliseconds(350));
                string screenshotPath = Path.Combine(screenshotDirectory, $"{screenId}.png");
                CaptureClientArea(hwnd, screenshotPath);

                AssertScreenshotPng(screenshotPath, ScreenshotWidth, ScreenshotHeight);
                AssertDoesNotIncludeNativeWindowChrome(screenshotPath);
            }

            ClientDebugActionResponse blankScreen = await client.NavigateToScreenAsync(
                new NavigateToScreenRequest { ScreenId = " " });
            ClientDebugActionResponse missingScreen = await client.NavigateToScreenAsync(
                new NavigateToScreenRequest { ScreenId = "missing-screen" });

            Assert.False(blankScreen.Success);
            Assert.False(missingScreen.Success);
            Assert.All(ScreenIds, screenId =>
            {
                string screenshotPath = Path.Combine(screenshotDirectory, $"{screenId}.png");
                Assert.True(File.Exists(screenshotPath), $"Missing screenshot for {screenId}.");
            });

            if (process.HasExited)
            {
                string stdout = await stdoutTask;
                string stderr = await stderrTask;
                Assert.Fail($"RiskyStars.Client exited during screenshot validation. stdout: {stdout} stderr: {stderr}");
            }
        }
        finally
        {
            if (process != null)
            {
                StopClient(process);
            }

            RestoreFile(settingsPath, originalSettings);
            RestoreFile(preferencesPath, originalPreferences);
        }
    }

    [Theory]
    [InlineData("RiskyStars.Tests/InteractionStateExpectations/main-menu-settings-click.yaml")]
    [InlineData("RiskyStars.Tests/InteractionStateExpectations/main-menu-settings-back-click.yaml")]
    [InlineData("RiskyStars.Tests/InteractionStateExpectations/main-menu-settings-save-click.yaml")]
    public async Task ValidationSequence_CapturesConfiguredInteractionScreenshotsAndComparesVisualTreeState(string expectationPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        LiveClientInteractionExpectation expectation = LiveClientInteractionExpectation.Load(expectationPath);
        string repositoryRoot = FindRepositoryRoot();
        string clientOutputDirectory = Path.Combine(repositoryRoot, "RiskyStars.Client", "bin", "Debug", "net9.0");
        string clientExecutable = Path.Combine(clientOutputDirectory, "RiskyStars.Client.exe");
        Assert.True(File.Exists(clientExecutable), $"Build the client before running interaction screenshot validation. Missing: {clientExecutable}");

        string screenshotDirectory = Path.Combine(repositoryRoot, "RiskyStars.Client", "Screenshots", "Interactions");
        Directory.CreateDirectory(screenshotDirectory);

        string beforeScreenshotPath = Path.Combine(screenshotDirectory, $"{expectation.InteractionId}-before.png");
        string afterScreenshotPath = Path.Combine(screenshotDirectory, $"{expectation.InteractionId}-after.png");
        string beforeTreePath = Path.Combine(screenshotDirectory, $"{expectation.InteractionId}-before.visual-tree.json");
        string afterTreePath = Path.Combine(screenshotDirectory, $"{expectation.InteractionId}-after.visual-tree.json");

        int debugPort = GetAvailableTcpPort();
        string settingsPath = Path.Combine(clientOutputDirectory, "settings.json");
        string preferencesPath = Path.Combine(clientOutputDirectory, "window_preferences.json");
        string? originalSettings = ReadExistingFile(settingsPath);
        string? originalPreferences = ReadExistingFile(preferencesPath);

        Process? process = null;
        try
        {
            WriteDeterministicClientSettings(settingsPath);
            File.WriteAllText(preferencesPath, "{}");

            process = StartClient(clientExecutable, clientOutputDirectory, debugPort);
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            IntPtr hwnd = await WaitForMainWindowHandleAsync(process, TimeSpan.FromSeconds(30));
            MoveClientAreaToMonitorWorkArea(hwnd);

            using var channel = GrpcChannel.ForAddress(
                $"http://localhost:{debugPort.ToString(CultureInfo.InvariantCulture)}",
                new GrpcChannelOptions
                {
                    HttpHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true
                    }
                });
            var client = new ClientDebugProtocol.ClientDebugProtocolClient(channel);
            await WaitForDebugProtocolAsync(client, TimeSpan.FromSeconds(30));

            ClientDebugActionResponse navigate = await client.NavigateToScreenAsync(
                new NavigateToScreenRequest { ScreenId = expectation.StartScreenId });
            Assert.True(navigate.Success, navigate.Message);

            string beforeJson = await WaitForVisualTreeTextAsync(client, expectation.BeforeContainsText[0], TimeSpan.FromSeconds(10));
            expectation.AssertBeforeMatches(beforeJson);
            await File.WriteAllTextAsync(beforeTreePath, beforeJson);
            CaptureClientArea(hwnd, beforeScreenshotPath);

            string settingsButtonId = FindElementIdByText(beforeJson, expectation.ActionText);
            ClientDebugActionResponse click = await client.InvokeClickAsync(
                new ClientDebugElementRequest { ElementId = settingsButtonId });
            Assert.True(click.Success, click.Message);

            string afterJson = await WaitForVisualTreeTextAsync(client, expectation.AfterContainsText[0], TimeSpan.FromSeconds(10));
            expectation.AssertAfterMatches(afterJson);
            expectation.AssertStateChanges(beforeJson, afterJson);
            await File.WriteAllTextAsync(afterTreePath, afterJson);
            CaptureClientArea(hwnd, afterScreenshotPath);

            AssertScreenshotPng(beforeScreenshotPath, ScreenshotWidth, ScreenshotHeight);
            AssertScreenshotPng(afterScreenshotPath, ScreenshotWidth, ScreenshotHeight);
            AssertDoesNotIncludeNativeWindowChrome(beforeScreenshotPath);
            AssertDoesNotIncludeNativeWindowChrome(afterScreenshotPath);
            Assert.NotEqual(File.ReadAllBytes(beforeScreenshotPath), File.ReadAllBytes(afterScreenshotPath));
            Assert.NotEqual(beforeJson, afterJson);

            if (process.HasExited)
            {
                string stdout = await stdoutTask;
                string stderr = await stderrTask;
                Assert.Fail($"RiskyStars.Client exited during interaction screenshot validation. stdout: {stdout} stderr: {stderr}");
            }
        }
        finally
        {
            if (process != null)
            {
                StopClient(process);
            }

            RestoreFile(settingsPath, originalSettings);
            RestoreFile(preferencesPath, originalPreferences);
        }
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public void HwndCapture_RejectsInvalidWindowHandleAsBadBehavior()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string tempPath = Path.Combine(Path.GetTempPath(), $"risky-stars-invalid-hwnd-{Guid.NewGuid():N}.png");

        var failure = Assert.Throws<InvalidOperationException>(() => CaptureClientArea(IntPtr.Zero, tempPath));

        Assert.Contains("window handle", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(tempPath));
    }

    [Fact]
    public void ClientAreaWorkAreaValidation_AcceptsClientAreaInsideWorkArea()
    {
        var captureRect = new NativeRect { Left = 0, Top = 0, Right = ScreenshotWidth, Bottom = ScreenshotHeight };
        var workArea = new NativeRect { Left = 0, Top = 0, Right = ScreenshotWidth, Bottom = ScreenshotHeight };

        AssertScreenRectFitsWorkArea(captureRect, workArea);
    }

    [Fact]
    public void ClientAreaWorkAreaValidation_RejectsTaskbarIntersectionAsBadBehavior()
    {
        var captureRect = new NativeRect { Left = 0, Top = 32, Right = ScreenshotWidth, Bottom = ScreenshotHeight + 32 };
        var workArea = new NativeRect { Left = 0, Top = 0, Right = ScreenshotWidth, Bottom = ScreenshotHeight };

        var failure = Assert.Throws<InvalidOperationException>(() => AssertScreenRectFitsWorkArea(captureRect, workArea));

        Assert.Contains("taskbar", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Process StartClient(string clientExecutable, string workingDirectory, int debugPort)
    {
        var startInfo = new ProcessStartInfo(clientExecutable)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.Environment["RISKYSTARS_CLIENT_DEBUG_PORT"] = debugPort.ToString(CultureInfo.InvariantCulture);

        Process? process = Process.Start(startInfo);
        Assert.NotNull(process);
        return process;
    }

    private static async Task WaitForDebugProtocolAsync(
        ClientDebugProtocol.ClientDebugProtocolClient client,
        TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                DumpVisualTreeResponse response = await client.DumpVisualTreeAsync(
                    new DumpVisualTreeRequest { IncludeHidden = true },
                    deadline: DateTime.UtcNow.AddSeconds(1)).ResponseAsync;
                if (response.Result.Success)
                {
                    return;
                }
            }
            catch (RpcException exception)
                when (exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.Cancelled)
            {
            }
            catch (IOException)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException("Client debug gRPC protocol did not respond before the timeout.");
    }

    private static async Task<string> WaitForVisualTreeTextAsync(
        ClientDebugProtocol.ClientDebugProtocolClient client,
        string expectedText,
        TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            DumpVisualTreeResponse response = await client.DumpVisualTreeAsync(
                new DumpVisualTreeRequest { IncludeHidden = true },
                deadline: DateTime.UtcNow.AddSeconds(1)).ResponseAsync;

            Assert.True(response.Result.Success, response.Result.Message);
            if (VisualTreeContainsText(response.Json, expectedText))
            {
                return response.Json;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException($"Visual tree never contained '{expectedText}' before the timeout.");
    }

    private static bool VisualTreeContainsText(string visualTreeJson, string text)
    {
        using JsonDocument document = JsonDocument.Parse(visualTreeJson);
        return VisualTreeContainsText(document.RootElement, text);
    }

    private static bool VisualTreeContainsText(JsonElement root, string text)
    {
        return root
            .GetProperty("elements")
            .EnumerateArray()
            .Any(element =>
                element.TryGetProperty("text", out JsonElement textElement) &&
                string.Equals(textElement.GetString(), text, StringComparison.OrdinalIgnoreCase));
    }

    private static string FindElementIdByText(string visualTreeJson, string text)
    {
        using JsonDocument document = JsonDocument.Parse(visualTreeJson);
        Dictionary<string, JsonElement> elementsById = document.RootElement
            .GetProperty("elements")
            .EnumerateArray()
            .ToDictionary(
                element => element.GetProperty("id").GetString() ?? string.Empty,
                element => element.Clone(),
                StringComparer.Ordinal);

        foreach (JsonElement element in elementsById.Values
                     .Where(element =>
                         element.TryGetProperty("text", out JsonElement textElement) &&
                         string.Equals(textElement.GetString(), text, StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(IsTreeVisible)
                     .ThenBy(element => element.GetProperty("id").GetString(), StringComparer.Ordinal))
        {
            string? clickableId = FindButtonAncestorId(element, elementsById);
            if (!string.IsNullOrWhiteSpace(clickableId))
            {
                return clickableId;
            }
        }

        throw new InvalidDataException($"No clickable visual tree element with text '{text}' was exported.");
    }

    private static bool IsTreeVisible(JsonElement element)
    {
        return element.TryGetProperty("treeVisible", out JsonElement treeVisibleElement) &&
               treeVisibleElement.GetBoolean();
    }

    private static string? FindButtonAncestorId(
        JsonElement element,
        IReadOnlyDictionary<string, JsonElement> elementsById)
    {
        JsonElement current = element;
        while (true)
        {
            string typeName = current.TryGetProperty("typeName", out JsonElement typeNameElement)
                ? typeNameElement.GetString() ?? string.Empty
                : string.Empty;
            if (typeName.Contains("Button", StringComparison.OrdinalIgnoreCase))
            {
                return current.GetProperty("id").GetString();
            }

            if (!current.TryGetProperty("parentId", out JsonElement parentIdElement))
            {
                return null;
            }

            string? parentId = parentIdElement.GetString();
            if (string.IsNullOrWhiteSpace(parentId) ||
                !elementsById.TryGetValue(parentId, out current))
            {
                return null;
            }
        }
    }

    private static async Task<IntPtr> WaitForMainWindowHandleAsync(Process process, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            process.Refresh();
            if (process.HasExited)
            {
                throw new InvalidOperationException($"RiskyStars.Client exited before creating a window. Exit code: {process.ExitCode}");
            }

            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException("RiskyStars.Client did not expose a main window handle.");
    }

    [SupportedOSPlatform("windows")]
    private static void CaptureClientArea(IntPtr hwnd, string outputPath)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("Cannot capture a client area without a valid window handle.");
        }

        IntPtr previousDpiContext = SetThreadDpiAwarenessContext(DpiAwarenessContextPerMonitorAwareV2);
        try
        {
            if (!GetClientRect(hwnd, out NativeRect clientRect))
            {
                throw new InvalidOperationException("GetClientRect failed for the RiskyStars.Client window handle.");
            }

            int width = clientRect.Right - clientRect.Left;
            int height = clientRect.Bottom - clientRect.Top;
            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException($"RiskyStars.Client window has an invalid client area: {width}x{height}.");
            }

            var origin = new NativePoint { X = 0, Y = 0 };
            if (!ClientToScreen(hwnd, ref origin))
            {
                throw new InvalidOperationException("ClientToScreen failed for the RiskyStars.Client window handle.");
            }

            var captureRect = new NativeRect
            {
                Left = origin.X,
                Top = origin.Y,
                Right = origin.X + width,
                Bottom = origin.Y + height
            };
            AssertScreenRectFitsWorkArea(captureRect, GetMonitorWorkArea(hwnd));

            SetForegroundWindow(hwnd);
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(origin.X, origin.Y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }

            if (width == ScreenshotWidth && height == ScreenshotHeight)
            {
                bitmap.Save(outputPath, ImageFormat.Png);
                return;
            }

            using var normalized = new Bitmap(bitmap, new Size(ScreenshotWidth, ScreenshotHeight));
            normalized.Save(outputPath, ImageFormat.Png);
        }
        finally
        {
            if (previousDpiContext != IntPtr.Zero)
            {
                SetThreadDpiAwarenessContext(previousDpiContext);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static void MoveClientAreaToMonitorWorkArea(IntPtr hwnd)
    {
        ShowWindow(hwnd, ShowWindowCommands.Restore);
        SetWindowPos(
            hwnd,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SetWindowPositionFlags.NoZOrder |
            SetWindowPositionFlags.NoSize |
            SetWindowPositionFlags.ShowWindow);

        if (!GetWindowRect(hwnd, out NativeRect windowRect))
        {
            throw new InvalidOperationException("GetWindowRect failed while positioning the RiskyStars.Client window.");
        }

        NativeRect workArea = GetMonitorWorkArea(hwnd);
        var clientOrigin = new NativePoint { X = 0, Y = 0 };
        if (!ClientToScreen(hwnd, ref clientOrigin))
        {
            throw new InvalidOperationException("ClientToScreen failed while positioning the RiskyStars.Client window.");
        }

        int targetWindowX = windowRect.Left + workArea.Left - clientOrigin.X;
        int targetWindowY = windowRect.Top + workArea.Top - clientOrigin.Y;
        SetWindowPos(
            hwnd,
            IntPtr.Zero,
            targetWindowX,
            targetWindowY,
            0,
            0,
            SetWindowPositionFlags.NoZOrder |
            SetWindowPositionFlags.NoSize |
            SetWindowPositionFlags.ShowWindow);
        SetForegroundWindow(hwnd);
    }

    private static void AssertScreenshotPng(string screenshotPath, int expectedWidth, int expectedHeight)
    {
        Assert.True(File.Exists(screenshotPath), $"Screenshot was not written: {screenshotPath}");
        var fileInfo = new FileInfo(screenshotPath);
        Assert.True(fileInfo.Length > 4096, $"{screenshotPath} is too small to be a real screen capture.");

        byte[] bytes = File.ReadAllBytes(screenshotPath);
        Assert.True(bytes.Length >= 24, $"{screenshotPath} is not a complete PNG file.");
        Assert.Equal([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], bytes.Take(8).ToArray());

        int width = ReadBigEndianInt32(bytes, 16);
        int height = ReadBigEndianInt32(bytes, 20);
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
    }

    [SupportedOSPlatform("windows")]
    private static void AssertDoesNotIncludeNativeWindowChrome(string screenshotPath)
    {
        using var bitmap = new Bitmap(screenshotPath);
        int sampledRows = Math.Min(24, bitmap.Height);
        long red = 0;
        long green = 0;
        long blue = 0;
        for (int y = 0; y < sampledRows; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color color = bitmap.GetPixel(x, y);
                red += color.R;
                green += color.G;
                blue += color.B;
            }
        }

        double count = sampledRows * bitmap.Width;
        double averageRed = red / count;
        double averageGreen = green / count;
        double averageBlue = blue / count;
        double averageBrightness = (averageRed + averageGreen + averageBlue) / 3.0;
        double channelSpread = Math.Max(averageRed, Math.Max(averageGreen, averageBlue)) -
                               Math.Min(averageRed, Math.Min(averageGreen, averageBlue));

        bool looksLikeNativeChrome = averageBrightness > 45 && averageBrightness < 95 && channelSpread < 12;
        Assert.False(
            looksLikeNativeChrome,
            $"{screenshotPath} appears to include the native Windows title bar instead of only the game client area.");
    }

    private static void AssertScreenRectFitsWorkArea(NativeRect captureRect, NativeRect workArea)
    {
        bool fitsWorkArea = captureRect.Left >= workArea.Left &&
                            captureRect.Top >= workArea.Top &&
                            captureRect.Right <= workArea.Right &&
                            captureRect.Bottom <= workArea.Bottom;

        if (!fitsWorkArea)
        {
            throw new InvalidOperationException(
                $"The HWND client capture rectangle {FormatRect(captureRect)} extends outside the monitor work area {FormatRect(workArea)} and would include desktop chrome such as the taskbar.");
        }
    }

    private static string FormatRect(NativeRect rect)
    {
        return $"{rect.Right - rect.Left}x{rect.Bottom - rect.Top} @ {rect.Left},{rect.Top}";
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24) |
               (bytes[offset + 1] << 16) |
               (bytes[offset + 2] << 8) |
               bytes[offset + 3];
    }

    private static int GetAvailableTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void WriteDeterministicClientSettings(string settingsPath)
    {
        File.WriteAllText(
            settingsPath,
            $$"""
            {
              "Theme": {
                "AccentColor": "Classic Green",
                "WarningColor": "Ivory",
                "FontStyle": "Standard",
                "FontScalePercent": 100,
                "PaddingScalePercent": 100,
                "FramePaddingPercent": 100,
                "ContrastPercent": 100
              },
              "ServerAddress": "http://localhost:5000",
              "ResolutionWidth": {{ScreenshotWidth.ToString(CultureInfo.InvariantCulture)}},
              "ResolutionHeight": {{ScreenshotHeight.ToString(CultureInfo.InvariantCulture)}},
              "UiScalePercent": 100,
              "WindowMode": "Normal",
              "Fullscreen": false,
              "VSync": true,
              "TargetFrameRate": 60,
              "MasterVolume": 1,
              "MusicVolume": 0.7,
              "SfxVolume": 0.8,
              "CameraPanSpeed": 5,
              "CameraZoomSpeed": 0.1,
              "InvertCameraZoom": false,
              "ShowDebugInfo": false,
              "ShowFPS": true,
              "MapCamera": {
                "HasSavedView": false,
                "PositionX": 0,
                "PositionY": 0,
                "Zoom": 1
              }
            }
            """);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RiskyStars.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the RiskyStars repository root.");
    }

    private static string? ReadExistingFile(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private static void RestoreFile(string path, string? originalContent)
    {
        if (originalContent == null)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return;
        }

        File.WriteAllText(path, originalContent);
    }

    private static void StopClient(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            process.CloseMainWindow();
            if (!process.WaitForExit(5000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        finally
        {
            process.Dispose();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hWnd, out NativeRect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref NativeMonitorInfo monitorInfo);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    private static readonly IntPtr DpiAwarenessContextPerMonitorAwareV2 = new(-4);

    private static class ShowWindowCommands
    {
        public const int Restore = 9;
    }

    private static class SetWindowPositionFlags
    {
        public const uint NoSize = 0x0001;
        public const uint NoZOrder = 0x0004;
        public const uint ShowWindow = 0x0040;
    }

    private static class MonitorDefaults
    {
        public const uint MonitorDefaultToNearest = 0x00000002;
    }

    [SupportedOSPlatform("windows")]
    private static NativeRect GetMonitorWorkArea(IntPtr hwnd)
    {
        IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaults.MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            throw new InvalidOperationException("MonitorFromWindow failed for the RiskyStars.Client window.");
        }

        var info = new NativeMonitorInfo
        {
            Size = Marshal.SizeOf<NativeMonitorInfo>()
        };
        if (!GetMonitorInfo(monitor, ref info))
        {
            throw new InvalidOperationException("GetMonitorInfo failed for the RiskyStars.Client window monitor.");
        }

        return info.WorkArea;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct NativeMonitorInfo
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public uint Flags;
    }

    private sealed class LiveClientInteractionExpectation
    {
        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        public string InteractionId { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string StartScreenId { get; set; } = string.Empty;

        public string ActionText { get; set; } = string.Empty;

        public List<string> BeforeContainsText { get; set; } = [];

        public List<string> BeforeRejectsText { get; set; } = [];

        public List<string> AfterContainsText { get; set; } = [];

        public List<string> AfterRejectsText { get; set; } = [];

        public List<LiveClientInteractionStateChange> StateChanges { get; set; } = [];

        public List<LiveClientInteractionBadBehavior> BadBehaviorValidations { get; set; } = [];

        public static LiveClientInteractionExpectation Load(string relativePath)
        {
            string fullPath = Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(fullPath), $"Missing live interaction expectation: {relativePath}");

            LiveClientInteractionExpectation expectation = Deserializer.Deserialize<LiveClientInteractionExpectation>(
                    File.ReadAllText(fullPath))
                ?? throw new InvalidDataException($"Live interaction expectation {relativePath} did not deserialize.");
            expectation.Validate();
            return expectation;
        }

        public void AssertBeforeMatches(string visualTreeJson)
        {
            using JsonDocument document = JsonDocument.Parse(visualTreeJson);
            AssertContainsAll(document.RootElement, BeforeContainsText, "before");
            AssertContainsNone(document.RootElement, BeforeRejectsText, "before");
        }

        public void AssertAfterMatches(string visualTreeJson)
        {
            using JsonDocument document = JsonDocument.Parse(visualTreeJson);
            AssertContainsAll(document.RootElement, AfterContainsText, "after");
            AssertContainsNone(document.RootElement, AfterRejectsText, "after");

            foreach (LiveClientInteractionBadBehavior badBehavior in BadBehaviorValidations)
            {
                bool actualPresent = VisualTreeContainsText(document.RootElement, badBehavior.Text);
                if (string.Equals(badBehavior.Phase, "after", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.NotEqual(badBehavior.RejectedPresent, actualPresent);
                }
            }
        }

        public void AssertStateChanges(string beforeJson, string afterJson)
        {
            using JsonDocument beforeDocument = JsonDocument.Parse(beforeJson);
            using JsonDocument afterDocument = JsonDocument.Parse(afterJson);
            foreach (LiveClientInteractionStateChange change in StateChanges)
            {
                Assert.Equal(change.BeforePresent, VisualTreeContainsText(beforeDocument.RootElement, change.BeforeText));
                Assert.Equal(change.AfterPresent, VisualTreeContainsText(afterDocument.RootElement, change.AfterText));
            }
        }

        private void Validate()
        {
            Assert.False(string.IsNullOrWhiteSpace(InteractionId), "interactionId is required.");
            Assert.False(string.IsNullOrWhiteSpace(StartScreenId), "startScreenId is required.");
            Assert.False(string.IsNullOrWhiteSpace(ActionText), "actionText is required.");
            Assert.NotEmpty(BeforeContainsText);
            Assert.NotEmpty(AfterContainsText);
            Assert.NotEmpty(StateChanges);
            Assert.NotEmpty(BadBehaviorValidations);
        }

        private static void AssertContainsAll(JsonElement root, IEnumerable<string> expectedTexts, string phase)
        {
            foreach (string expectedText in expectedTexts)
            {
                Assert.True(
                    VisualTreeContainsText(root, expectedText),
                    $"Expected {phase} visual tree to contain '{expectedText}'.");
            }
        }

        private static void AssertContainsNone(JsonElement root, IEnumerable<string> rejectedTexts, string phase)
        {
            foreach (string rejectedText in rejectedTexts)
            {
                Assert.False(
                    VisualTreeContainsText(root, rejectedText),
                    $"Expected {phase} visual tree not to contain '{rejectedText}'.");
            }
        }
    }

    private sealed class LiveClientInteractionStateChange
    {
        public string Description { get; set; } = string.Empty;

        public string BeforeText { get; set; } = string.Empty;

        public bool BeforePresent { get; set; }

        public string AfterText { get; set; } = string.Empty;

        public bool AfterPresent { get; set; }
    }

    private sealed class LiveClientInteractionBadBehavior
    {
        public string Text { get; set; } = string.Empty;

        public string Phase { get; set; } = string.Empty;

        public bool RejectedPresent { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
