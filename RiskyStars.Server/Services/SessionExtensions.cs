using Grpc.Core;

namespace RiskyStars.Server.Services;

public static class SessionExtensions
{
    public static string? GetAuthToken(this ServerCallContext context)
    {
        var authHeader = context.RequestHeaders.GetValue("authorization");
        if (string.IsNullOrEmpty(authHeader))
            return null;

        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader.Substring(7);

        return authHeader;
    }

    public static string? GetSessionId(this ServerCallContext context)
    {
        return context.RequestHeaders.GetValue("session-id");
    }

    public static bool TryAuthenticatePlayer(
        this ServerCallContext context,
        GameSessionManager sessionManager,
        out string playerId)
    {
        playerId = string.Empty;
        
        var authToken = context.GetAuthToken();
        if (string.IsNullOrEmpty(authToken))
            return false;

        return sessionManager.ValidateAuthToken(authToken, out playerId);
    }

    public static void ThrowIfNotAuthenticated(
        this ServerCallContext context,
        GameSessionManager sessionManager,
        out string playerId)
    {
        if (!context.TryAuthenticatePlayer(sessionManager, out playerId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid or missing authentication token"));
        }
    }
}
