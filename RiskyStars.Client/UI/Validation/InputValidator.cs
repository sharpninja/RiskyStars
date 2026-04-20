using System;
using System.Text.RegularExpressions;

namespace RiskyStars.Client;

/// <summary>
/// Provides comprehensive input validation for UI forms
/// </summary>
public static class InputValidator
{
    // Server address validation
    public static ValidationResult ValidateServerAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return new ValidationResult(false, "Server address cannot be empty");
        }

        var trimmed = address.Trim();

        // Check for valid URL format
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return new ValidationResult(false, "Invalid server address format. Use http://host:port or https://host:port");
        }

        // Check for http or https scheme
        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            return new ValidationResult(false, "Server address must use http:// or https://");
        }

        // Check for valid host
        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            return new ValidationResult(false, "Server address must include a valid host");
        }

        return new ValidationResult(true, "Valid server address");
    }

    // Player name validation
    public static ValidationResult ValidatePlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new ValidationResult(false, "Player name cannot be empty");
        }

        var trimmed = name.Trim();

        if (trimmed.Length < 2)
        {
            return new ValidationResult(false, "Player name must be at least 2 characters");
        }

        if (trimmed.Length > 20)
        {
            return new ValidationResult(false, "Player name must be 20 characters or less");
        }

        // Check for valid characters (alphanumeric, spaces, underscores, hyphens)
        if (!Regex.IsMatch(trimmed, @"^[a-zA-Z0-9 _-]+$"))
        {
            return new ValidationResult(false, "Player name can only contain letters, numbers, spaces, underscores, and hyphens");
        }

        // Check for at least one letter or number
        if (!Regex.IsMatch(trimmed, @"[a-zA-Z0-9]"))
        {
            return new ValidationResult(false, "Player name must contain at least one letter or number");
        }

        return new ValidationResult(true, "Valid player name");
    }

    // Map name validation
    public static ValidationResult ValidateMapName(string mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName))
        {
            return new ValidationResult(false, "Map name cannot be empty");
        }

        var trimmed = mapName.Trim();

        if (trimmed.Length < 2)
        {
            return new ValidationResult(false, "Map name must be at least 2 characters");
        }

        if (trimmed.Length > 50)
        {
            return new ValidationResult(false, "Map name must be 50 characters or less");
        }

        // Check for valid characters
        if (!Regex.IsMatch(trimmed, @"^[a-zA-Z0-9 _-]+$"))
        {
            return new ValidationResult(false, "Map name can only contain letters, numbers, spaces, underscores, and hyphens");
        }

        return new ValidationResult(true, "Valid map name");
    }

    // Lobby player count validation
    public static ValidationResult ValidatePlayerCount(int count, int min, int max)
    {
        if (count < min)
        {
            return new ValidationResult(false, $"Player count must be at least {min}");
        }

        if (count > max)
        {
            return new ValidationResult(false, $"Player count cannot exceed {max}");
        }

        return new ValidationResult(true, "Valid player count");
    }

    // Generic numeric range validation
    public static ValidationResult ValidateNumericRange(int value, int min, int max, string fieldName)
    {
        if (value < min)
        {
            return new ValidationResult(false, $"{fieldName} must be at least {min}");
        }

        if (value > max)
        {
            return new ValidationResult(false, $"{fieldName} cannot exceed {max}");
        }

        return new ValidationResult(true, $"Valid {fieldName}");
    }

    // Generic required field validation
    public static ValidationResult ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ValidationResult(false, $"{fieldName} is required");
        }

        return new ValidationResult(true, $"Valid {fieldName}");
    }
}

/// <summary>
/// Result of input validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; }
    public string Message { get; }

    public ValidationResult(bool isValid, string message)
    {
        IsValid = isValid;
        Message = message;
    }
}
