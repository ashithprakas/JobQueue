using System.Diagnostics;

namespace JobQueue.Core.Models;

public record StreamJobEntry(string EntryId, Guid JobId, string? TraceId);
