using System.Collections.Concurrent;

namespace RiskyStars.Client;

public enum GameFeedbackSeverity
{
    Info,
    Success,
    Warning,
    Error,
    Busy
}

public sealed record GameFeedbackMessage(
    string Title,
    string? Detail,
    GameFeedbackSeverity Severity,
    bool Sticky,
    DateTime TimestampUtc);

public static class GameFeedbackBus
{
    private static readonly ConcurrentQueue<GameFeedbackMessage> Messages = new();

    public static void PublishInfo(string title, string? detail = null, bool sticky = false) =>
        Publish(title, detail, GameFeedbackSeverity.Info, sticky);

    public static void PublishSuccess(string title, string? detail = null, bool sticky = false) =>
        Publish(title, detail, GameFeedbackSeverity.Success, sticky);

    public static void PublishWarning(string title, string? detail = null, bool sticky = false) =>
        Publish(title, detail, GameFeedbackSeverity.Warning, sticky);

    public static void PublishError(string title, string? detail = null, bool sticky = true) =>
        Publish(title, detail, GameFeedbackSeverity.Error, sticky);

    public static void PublishBusy(string title, string? detail = null, bool sticky = false) =>
        Publish(title, detail, GameFeedbackSeverity.Busy, sticky);

    public static void Publish(string title, string? detail, GameFeedbackSeverity severity, bool sticky = false)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        Messages.Enqueue(new GameFeedbackMessage(title.Trim(), detail?.Trim(), severity, sticky, DateTime.UtcNow));
    }

    public static bool TryDequeue(out GameFeedbackMessage message) => Messages.TryDequeue(out message!);

    public static void Clear()
    {
        while (Messages.TryDequeue(out _))
        {
        }
    }
}
