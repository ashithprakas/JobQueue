using System.Diagnostics;

namespace JobQueue.Core.Telemetry.Config;

public static class DiagnosticConfig
{
    private const string SourceName = "JobQueue.Job";
    public static readonly ActivitySource ActivitySource = new(SourceName);
}
