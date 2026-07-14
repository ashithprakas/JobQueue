namespace JobQueue.Core.Constants;

public static class JobConstants
{
    public const int MaxAttempts = 3;
    public const int MaxPayloadLength = 262144;
    public const int JobProcessCount = 5;
}