namespace ListServDB.Orchestration;

/// <summary>
/// Provides constant message types used in the orchestration process.
/// </summary>
public static class MessageTypes
{
    /// <summary>
    /// Message type indicating that an API key is to be revoked.
    /// </summary>
    public const string RevokeApiKey = "RevokeApiKey";

    /// <summary>
    /// Message type indicating that an API key is to be enlisted.
    /// </summary>
    public const string EnlistApiKey = "EnlistApiKey";
}
