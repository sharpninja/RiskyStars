using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace RiskyStars.Tools;

public class GenerateSpritePlaceholders
{
    private static readonly string ContentPath = Path.Combine("..", "..", "..", "Content", "Sprites");

    public static void Main(string[] args)
    {
        Console.WriteLine("Generating sprite placeholders...");

        Directory.CreateDirectory(ContentPath);
        
        GenerateStellarBodies();
        GenerateArmies();
        GenerateUI();
        GenerateHyperspaceLanes();
        GenerateCombat();

        Console.WriteLine("All placeholders generated successfully!");
    }

    private static void GenerateStellarBodies()
    {
        var dir = Path.Combine(ContentPath, "StellarBodies");
        Directory.CreateDirectory(dir);

        Console.WriteLine("Generating stellar body sprites...");

        // Gas Giant - orange/brown with bands
        using (var bmp = new Bitmap(64, 64))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new SolidBrush(Color.FromArgb(200, 150, 100)))
            {
                g.FillEllipse(brush, 2, 2, 60, 60);
            }
            
            // Bands
            using (var pen = new Pen(Color.FromArgb(180, 130, 80), 3))
            {
                g.DrawLine(pen, 10, 24, 54, 24);
                g.DrawLine(pen, 10, 32, 54, 32);
                g.DrawLine(pen, 10, 40, 54, 40);
            }
            
            SavePng(bmp, Path.Combine(dir, "GasGiant.png"));
        }

        // Gas Giant Variant 1 - more yellow
        using (var bmp = new Bitmap(64, 64))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new SolidBrush(Color.FromArgb(220, 180, 100)))
            {
                g.FillEllipse(brush, 2, 2, 60, 60);
            }
            
            using (var pen = new Pen(Color.FromArgb(200, 160, 80), 3))
            {
                g.DrawLine(pen, 10, 20, 54, 20);
                g.DrawLine(pen, 10, 28, 54, 28);
                g.DrawLine(pen, 10, 36, 54, 36);
                g.DrawLine(pen, 10, 44, 54, 44);
            }
            
            SavePng(bmp, Path.Combine(dir, "GasGiant_Variant1.png"));
        }

        // Gas Giant Variant 2 - more reddish
        using (var bmp = new Bitmap(64, 64))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new SolidBrush(Color.FromArgb(220, 120, 90)))
            {
                g.FillEllipse(brush, 2, 2, 60, 60);
            }
            
            using (var pen = new Pen(Color.FromArgb(200, 100, 70), 3))
            {
                g.DrawLine(pen, 10, 22, 54, 22);
                g.DrawLine(pen, 10, 30, 54, 30);
                g.DrawLine(pen, 10, 38, 54, 38);
            }
            
            SavePng(bmp, Path.Combine(dir, "GasGiant_Variant2.png"));
        }

        // Rocky Planet - blue/green
        using (var bmp = new Bitmap(48, 48))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new SolidBrush(Color.FromArgb(100, 150, 200)))
            {
                g.FillEllipse(brush, 2, 2, 44, 44);
            }
            
            // Land masses
            using (var brush = new SolidBrush(Color.FromArgb(80, 180, 100)))
            {
                g.FillEllipse(brush, 8, 12, 18, 12);
                g.FillEllipse(brush, 24, 22, 14, 10);
            }
            
            SavePng(bmp, Path.Combine(dir, "RockyPlanet.png"));
        }

        // Rocky Planet Variant 1 - desert
        using (var bmp = new Bitmap(48, 48))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new SolidBrush(Color.FromArgb(200, 160, 100)))
            {
                g.FillEllipse(brush, 2, 2, 44, 44);
            }
            
            using (var brush = new SolidBrush(Color.FromArgb(180, 140, 80)))
            {
                g.FillEllipse(brush, 10, 15, 12, 8);
                g.FillEllipse(brush, 26, 20, 10, 6);
            }
            
            SavePng(bmp, Path.Combine(dir, "RockyPlanet_Variant1.png"));
        }

        // Rocky Planet Variant 2 - ice world
        using (var bmp = new Bitmap(48, 48))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new SolidBrush(Color.FromArgb(180, 200, 230)))
            {
                g.FillEllipse(brush, 2, 2, 44, 44);
            }
            
            using (var brush = new SolidBrush(Color.FromArgb(240, 250, 255)))
            {
                g.FillEllipse(brush, 12, 4, 20, 12);
                g.FillEllipse(brush, 8, 30, 24, 12);
            }
            
            SavePng(bmp, Path.Combine(dir, "RockyPlanet_Variant2.png"));
        }

        // Planetoid - small gray rock
        using (var bmp = new Bitmap(24, 24))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new SolidBrush(Color.FromArgb(150, 150, 150)))
            {
                g.FillEllipse(brush, 2, 2, 20, 20);
            }
            
            using (var brush = new SolidBrush(Color.FromArgb(120, 120, 120)))
            {
                g.FillEllipse(brush, 6, 6, 6, 6);
                g.FillEllipse(brush, 12, 10, 4, 4);
            }
            
            SavePng(bmp, Path.Combine(dir, "Planetoid.png"));
        }

        // Comet - small with tail
        using (var bmp = new Bitmap(32, 32))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            // Tail
            using (var brush = new LinearGradientBrush(
                new Point(4, 16),
                new Point(28, 16),
                Color.FromArgb(180, 150, 200, 255),
                Color.FromArgb(0, 150, 200, 255)))
            {
                g.FillEllipse(brush, 4, 12, 24, 8);
            }
            
            // Core
            using (var brush = new SolidBrush(Color.FromArgb(200, 220, 255)))
            {
                g.FillEllipse(brush, 20, 13, 10, 6);
            }
            
            SavePng(bmp, Path.Combine(dir, "Comet.png"));
        }
    }

    private static void GenerateArmies()
    {
        var dir = Path.Combine(ContentPath, "Armies");
        Directory.CreateDirectory(dir);

        Console.WriteLine("Generating army sprites...");

        // Army - generic unit icon
        using (var bmp = new Bitmap(32, 32))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            // Shield shape
            var points = new PointF[]
            {
                new PointF(16, 4),
                new PointF(28, 10),
                new PointF(28, 20),
                new PointF(16, 28),
                new PointF(4, 20),
                new PointF(4, 10)
            };
            
            using (var brush = new SolidBrush(Color.FromArgb(180, 180, 180)))
            {
                g.FillPolygon(brush, points);
            }
            
            using (var pen = new Pen(Color.FromArgb(100, 100, 100), 2))
            {
                g.DrawPolygon(pen, points);
            }
            
            // Star emblem
            using (var brush = new SolidBrush(Color.FromArgb(220, 220, 220)))
            {
                g.FillEllipse(brush, 13, 13, 6, 6);
            }
            
            SavePng(bmp, Path.Combine(dir, "Army.png"));
        }

        // Hero - special unit with crown
        using (var bmp = new Bitmap(32, 32))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            // Shield
            var points = new PointF[]
            {
                new PointF(16, 6),
                new PointF(26, 12),
                new PointF(26, 22),
                new PointF(16, 28),
                new PointF(6, 22),
                new PointF(6, 12)
            };
            
            using (var brush = new SolidBrush(Color.FromArgb(220, 180, 60)))
            {
                g.FillPolygon(brush, points);
            }
            
            using (var pen = new Pen(Color.FromArgb(180, 140, 20), 2))
            {
                g.DrawPolygon(pen, points);
            }
            
            // Crown
            var crown = new PointF[]
            {
                new PointF(12, 8),
                new PointF(14, 4),
                new PointF(16, 6),
                new PointF(18, 4),
                new PointF(20, 8),
                new PointF(20, 12),
                new PointF(12, 12)
            };
            
            using (var brush = new SolidBrush(Color.FromArgb(255, 215, 0)))
            {
                g.FillPolygon(brush, crown);
            }
            
            SavePng(bmp, Path.Combine(dir, "Hero.png"));
        }
    }

    private static void GenerateUI()
    {
        var dir = Path.Combine(ContentPath, "UI");
        Directory.CreateDirectory(dir);

        Console.WriteLine("Generating UI sprites...");

        // Button Normal
        using (var bmp = new Bitmap(120, 40))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, 120, 40),
                Color.FromArgb(80, 80, 100),
                Color.FromArgb(60, 60, 80),
                LinearGradientMode.Vertical))
            {
                g.FillRoundedRectangle(brush, 2, 2, 116, 36, 6);
            }
            
            using (var pen = new Pen(Color.FromArgb(120, 120, 140), 2))
            {
                g.DrawRoundedRectangle(pen, 2, 2, 116, 36, 6);
            }
            
            SavePng(bmp, Path.Combine(dir, "ButtonNormal.png"));
        }

        // Button Hover
        using (var bmp = new Bitmap(120, 40))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, 120, 40),
                Color.FromArgb(100, 100, 120),
                Color.FromArgb(80, 80, 100),
                LinearGradientMode.Vertical))
            {
                g.FillRoundedRectangle(brush, 2, 2, 116, 36, 6);
            }
            
            using (var pen = new Pen(Color.FromArgb(140, 140, 160), 2))
            {
                g.DrawRoundedRectangle(pen, 2, 2, 116, 36, 6);
            }
            
            SavePng(bmp, Path.Combine(dir, "ButtonHover.png"));
        }

        // Button Pressed
        using (var bmp = new Bitmap(120, 40))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, 120, 40),
                Color.FromArgb(60, 60, 80),
                Color.FromArgb(80, 80, 100),
                LinearGradientMode.Vertical))
            {
                g.FillRoundedRectangle(brush, 2, 2, 116, 36, 6);
            }
            
            using (var pen = new Pen(Color.FromArgb(100, 100, 120), 2))
            {
                g.DrawRoundedRectangle(pen, 2, 2, 116, 36, 6);
            }
            
            SavePng(bmp, Path.Combine(dir, "ButtonPressed.png"));
        }

        // Panel
        using (var bmp = new Bitmap(200, 150))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new SolidBrush(Color.FromArgb(200, 40, 40, 50)))
            {
                g.FillRoundedRectangle(brush, 0, 0, 200, 150, 8);
            }
            
            using (var pen = new Pen(Color.FromArgb(100, 100, 120), 3))
            {
                g.DrawRoundedRectangle(pen, 2, 2, 196, 146, 8);
            }
            
            SavePng(bmp, Path.Combine(dir, "Panel.png"));
        }

        // Icon Production - factory/gear
        using (var bmp = new Bitmap(32, 32))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var brush = new SolidBrush(Color.FromArgb(180, 140, 60)))
            {
                g.FillRectangle(brush, 8, 12, 16, 14);
            }
            
            using (var brush = new SolidBrush(Color.FromArgb(160, 120, 40)))
            {
                g.FillPolygon(brush, new Point[] {
                    new Point(12, 12),
                    new Point(20, 12),
                    new Point(24, 6),
                    new Point(8, 6)
                });
            }
            
            using (var pen = new Pen(Color.FromArgb(100, 80, 20), 2))
            {
                g.DrawRectangle(pen, 8, 12, 16, 14);
            }
            
            SavePng(bmp, Path.Combine(dir, "IconProduction.png"));
        }

        // Icon Energy - lightning bolt
        using (var bmp = new Bitmap(32, 32))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            var bolt = new Point[]
            {
                new Point(18, 4),
                new Point(14, 14),
                new Point(20, 14),
                new Point(12, 28),
                new Point(16, 18),
                new Point(10, 18)
            };
            
            using (var brush = new SolidBrush(Color.FromArgb(255, 220, 60)))
            {
                g.FillPolygon(brush, bolt);
            }
            
            using (var pen = new Pen(Color.FromArgb(220, 180, 20), 2))
            {
                g.DrawPolygon(pen, bolt);
            }
            
            SavePng(bmp, Path.Combine(dir, "IconEnergy.png"));
        }
    }

    private static void GenerateHyperspaceLanes()
    {
        var dir = Path.Combine(ContentPath, "HyperspaceLanes");
        Directory.CreateDirectory(dir);

        Console.WriteLine("Generating hyperspace lane sprites...");

        // Lane - dashed line texture
        using (var bmp = new Bitmap(32, 8))
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            
            using (var brush = new SolidBrush(Color.FromArgb(150, 150, 150)))
            {
                g.FillRectangle(brush, 0, 3, 12, 2);
                g.FillRectangle(brush, 16, 3, 12, 2);
            }
            
            SavePng(bmp, Path.Combine(dir, "Lane.png"));
        }

        // Lane Mouth - portal/gate
        using (var bmp = new Bitmap(32, 32))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            // Outer ring
            using (var pen = new Pen(Color.FromArgb(100, 150, 200), 3))
            {
                g.DrawEllipse(pen, 4, 4, 24, 24);
            }
            
            // Inner glow
            using (var brush = new SolidBrush(Color.FromArgb(80, 120, 180, 220)))
            {
                g.FillEllipse(brush, 10, 10, 12, 12);
            }
            
            // Gate posts
            using (var brush = new SolidBrush(Color.FromArgb(140, 140, 160)))
            {
                g.FillRectangle(brush, 14, 2, 4, 8);
                g.FillRectangle(brush, 14, 22, 4, 8);
            }
            
            SavePng(bmp, Path.Combine(dir, "LaneMouth.png"));
        }
    }

    private static void GenerateCombat()
    {
        var dir = Path.Combine(ContentPath, "Combat");
        Directory.CreateDirectory(dir);

        Console.WriteLine("Generating combat sprites...");

        // Hit - impact flash
        using (var bmp = new Bitmap(32, 32))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            // Star burst
            using (var brush = new SolidBrush(Color.FromArgb(255, 200, 100)))
            {
                g.FillEllipse(brush, 12, 12, 8, 8);
            }
            
            using (var pen = new Pen(Color.FromArgb(255, 150, 50), 3))
            {
                g.DrawLine(pen, 16, 6, 16, 26);
                g.DrawLine(pen, 6, 16, 26, 16);
                g.DrawLine(pen, 9, 9, 23, 23);
                g.DrawLine(pen, 23, 9, 9, 23);
            }
            
            SavePng(bmp, Path.Combine(dir, "Hit.png"));
        }

        // Miss - X mark
        using (var bmp = new Bitmap(32, 32))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using (var pen = new Pen(Color.FromArgb(150, 150, 180), 4))
            {
                g.DrawLine(pen, 8, 8, 24, 24);
                g.DrawLine(pen, 24, 8, 8, 24);
            }
            
            SavePng(bmp, Path.Combine(dir, "Miss.png"));
        }

        // Explosion - burst effect
        using (var bmp = new Bitmap(48, 48))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            // Outer ring
            using (var brush = new SolidBrush(Color.FromArgb(150, 255, 100, 50)))
            {
                g.FillEllipse(brush, 8, 8, 32, 32);
            }
            
            // Middle ring
            using (var brush = new SolidBrush(Color.FromArgb(200, 255, 150, 50)))
            {
                g.FillEllipse(brush, 14, 14, 20, 20);
            }
            
            // Core
            using (var brush = new SolidBrush(Color.FromArgb(255, 255, 200, 100)))
            {
                g.FillEllipse(brush, 18, 18, 12, 12);
            }
            
            SavePng(bmp, Path.Combine(dir, "Explosion.png"));
        }

        // Dice Roll - six-sided die
        using (var bmp = new Bitmap(48, 48))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            // Die face
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, 48, 48),
                Color.FromArgb(240, 240, 240),
                Color.FromArgb(200, 200, 200),
                LinearGradientMode.Vertical))
            {
                g.FillRoundedRectangle(brush, 4, 4, 40, 40, 6);
            }
            
            using (var pen = new Pen(Color.FromArgb(160, 160, 160), 2))
            {
                g.DrawRoundedRectangle(pen, 4, 4, 40, 40, 6);
            }
            
            // Pip (showing one dot for placeholder)
            using (var brush = new SolidBrush(Color.FromArgb(60, 60, 60)))
            {
                g.FillEllipse(brush, 20, 20, 8, 8);
            }
            
            SavePng(bmp, Path.Combine(dir, "DiceRoll.png"));
        }
    }

    private static void SavePng(Bitmap bitmap, string path)
    {
        bitmap.Save(path, ImageFormat.Png);
        Console.WriteLine($"  Created: {Path.GetFileName(path)}");
    }
}

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
    {
        using (var path = GetRoundedRectPath(x, y, width, height, radius))
        {
            g.FillPath(brush, path);
        }
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, int x, int y, int width, int height, int radius)
    {
        using (var path = GetRoundedRectPath(x, y, width, height, radius))
        {
            g.DrawPath(pen, path);
        }
    }

    private static GraphicsPath GetRoundedRectPath(int x, int y, int width, int height, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
        path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        return path;
    }
}
