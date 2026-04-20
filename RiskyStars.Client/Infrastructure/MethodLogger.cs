using System;

namespace RiskyStars.Client;

public static class MethodLogger
{
    private static bool _enabled = true;
    private static LogLevel _level = LogLevel.Verbose;

    public enum LogLevel
    {
        None,
        ErrorOnly,
        Normal,
        Verbose
    }

    public static void SetEnabled(bool enabled) => _enabled = enabled;
    public static void SetLevel(LogLevel level) => _level = level;

    public static void LogEntry(string methodName, string details = "")
    {
        if (!_enabled || _level < LogLevel.Normal)
        {
            return;
        }

        string message = string.IsNullOrEmpty(details) 
            ? $"[Entry] {methodName}" 
            : $"[Entry] {methodName} - {details}";
        Console.WriteLine(message);
    }

    public static void LogExit(string methodName, string result = "")
    {
        if (!_enabled || _level < LogLevel.Normal)
        {
            return;
        }

        string message = string.IsNullOrEmpty(result) 
            ? $"[Exit] {methodName}" 
            : $"[Exit] {methodName} - {result}";
        Console.WriteLine(message);
    }

    public static void LogError(string methodName, string message, Exception? ex = null)
    {
        if (!_enabled || _level < LogLevel.ErrorOnly)
        {
            return;
        }

        Console.WriteLine($"[Error] {methodName}: {message}");
        if (ex != null)
        {
            Console.WriteLine($"[Error] Exception: {ex.GetType().Name} - {ex.Message}");
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                Console.WriteLine($"[Error] Stack: {ex.StackTrace}");
            }
        }
    }

    public static void LogProperty(string propertyName, string value)
    {
        if (!_enabled || _level < LogLevel.Verbose)
        {
            return;
        }

        Console.WriteLine($"[Property] {propertyName} = {value}");
    }

    public static void LogInfo(string message)
    {
        if (!_enabled || _level < LogLevel.Normal)
        {
            return;
        }

        Console.WriteLine($"[Info] {message}");
    }
}
