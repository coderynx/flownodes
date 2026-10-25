using System.Text.Json;

namespace Engine.Infrastructure.Resources;

internal interface IStatusReporter
{
    Task ReportAsync(
        string @namespace,
        string kind,
        string name,
        Guid uid,
        long observedGeneration,
        long statusSeq,
        JsonElement status);
}
