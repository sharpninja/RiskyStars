using System;
using System.IO;

namespace RiskyStars.Tools;

class CreatePlaceholders
{
    static void Main()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string contentPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Content", "Sprites"));
        
        Console.WriteLine($"Creating sprites in: {contentPath}");
        
        // Create directories
        Directory.CreateDirectory(Path.Combine(contentPath, "StellarBodies"));
        Directory.CreateDirectory(Path.Combine(contentPath, "Armies"));
        Directory.CreateDirectory(Path.Combine(contentPath, "UI"));
        Directory.CreateDirectory(Path.Combine(contentPath, "HyperspaceLanes"));
        Directory.CreateDirectory(Path.Combine(contentPath, "Combat"));
        
        Console.WriteLine("Creating placeholder PNG files...");
        
        // Stellar Bodies
        CreateMinimalPng(Path.Combine(contentPath, "StellarBodies", "GasGiant.png"));
        CreateMinimalPng(Path.Combine(contentPath, "StellarBodies", "GasGiant_Variant1.png"));
        CreateMinimalPng(Path.Combine(contentPath, "StellarBodies", "GasGiant_Variant2.png"));
        CreateMinimalPng(Path.Combine(contentPath, "StellarBodies", "RockyPlanet.png"));
        CreateMinimalPng(Path.Combine(contentPath, "StellarBodies", "RockyPlanet_Variant1.png"));
        CreateMinimalPng(Path.Combine(contentPath, "StellarBodies", "RockyPlanet_Variant2.png"));
        CreateMinimalPng(Path.Combine(contentPath, "StellarBodies", "Planetoid.png"));
        CreateMinimalPng(Path.Combine(contentPath, "StellarBodies", "Comet.png"));
        
        // Armies
        CreateMinimalPng(Path.Combine(contentPath, "Armies", "Army.png"));
        CreateMinimalPng(Path.Combine(contentPath, "Armies", "Hero.png"));
        
        // UI
        CreateMinimalPng(Path.Combine(contentPath, "UI", "ButtonNormal.png"));
        CreateMinimalPng(Path.Combine(contentPath, "UI", "ButtonHover.png"));
        CreateMinimalPng(Path.Combine(contentPath, "UI", "ButtonPressed.png"));
        CreateMinimalPng(Path.Combine(contentPath, "UI", "Panel.png"));
        CreateMinimalPng(Path.Combine(contentPath, "UI", "IconProduction.png"));
        CreateMinimalPng(Path.Combine(contentPath, "UI", "IconEnergy.png"));
        
        // Hyperspace Lanes
        CreateMinimalPng(Path.Combine(contentPath, "HyperspaceLanes", "Lane.png"));
        CreateMinimalPng(Path.Combine(contentPath, "HyperspaceLanes", "LaneMouth.png"));
        
        // Combat
        CreateMinimalPng(Path.Combine(contentPath, "Combat", "Hit.png"));
        CreateMinimalPng(Path.Combine(contentPath, "Combat", "Miss.png"));
        CreateMinimalPng(Path.Combine(contentPath, "Combat", "Explosion.png"));
        CreateMinimalPng(Path.Combine(contentPath, "Combat", "DiceRoll.png"));
        
        Console.WriteLine("\nAll placeholder PNG files created successfully!");
        Console.WriteLine($"Total files: 22");
    }
    
    static void CreateMinimalPng(string path)
    {
        // Minimal valid 1x1 transparent PNG
        byte[] pngData = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, // IHDR chunk length
            0x49, 0x48, 0x44, 0x52, // "IHDR"
            0x00, 0x00, 0x00, 0x01, // Width: 1
            0x00, 0x00, 0x00, 0x01, // Height: 1
            0x08, 0x06, 0x00, 0x00, 0x00, // Bit depth, color type, compression, filter, interlace
            0x1F, 0x15, 0xC4, 0x89, // CRC
            0x00, 0x00, 0x00, 0x0A, // IDAT chunk length
            0x49, 0x44, 0x41, 0x54, // "IDAT"
            0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01, // Compressed data
            0x0D, 0x0A, 0x2D, 0xB4, // CRC
            0x00, 0x00, 0x00, 0x00, // IEND chunk length
            0x49, 0x45, 0x4E, 0x44, // "IEND"
            0xAE, 0x42, 0x60, 0x82  // CRC
        };
        
        File.WriteAllBytes(path, pngData);
        Console.WriteLine($"  Created: {Path.GetFileName(path)}");
    }
}
