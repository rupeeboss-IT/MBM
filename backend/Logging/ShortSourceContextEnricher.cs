using Serilog.Core;
using Serilog.Events;

namespace RB_Website_API.Logging;

internal sealed class ShortSourceContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
            return;

        var fullName = sourceContext.ToString().Trim('"');
        if (string.IsNullOrWhiteSpace(fullName))
            return;

        var lastDot = fullName.LastIndexOf('.');
        var shortName = lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ShortSourceContext", shortName));
    }
}
