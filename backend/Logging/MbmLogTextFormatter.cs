using Serilog.Events;
using Serilog.Formatting;

namespace RB_Website_API.Logging;

/// <summary>
/// Formats log entries for daily rolling files: timestamp, level, class, message, context, exception.
/// </summary>
public sealed class MbmLogTextFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        output.WriteLine($"{logEvent.Timestamp.LocalDateTime:yyyy-MM-dd HH:mm:ss} [{ToLevelLabel(logEvent.Level)}]");
        output.WriteLine(ResolveSourceLabel(logEvent));
        output.WriteLine(SensitiveDataLogFilter.RedactMessage(logEvent.RenderMessage()));

        WriteContextLine(output, "Request URL", logEvent, "RequestUrl");
        WriteContextLine(output, "User ID", logEvent, "UserId");
        WriteContextLine(output, "IP Address", logEvent, "IpAddress");
        WriteContextLine(output, "Correlation ID", logEvent, "CorrelationId");

        if (logEvent.Exception is not null)
        {
            output.WriteLine("Exception:");
            output.WriteLine(logEvent.Exception);
        }

        output.WriteLine();
    }

    private static string ToLevelLabel(LogEventLevel level) =>
        level switch
        {
            LogEventLevel.Verbose => "TRACE",
            LogEventLevel.Debug => "DEBUG",
            LogEventLevel.Information => "INFO",
            LogEventLevel.Warning => "WARN",
            LogEventLevel.Error => "ERROR",
            LogEventLevel.Fatal => "CRITICAL",
            _ => level.ToString().ToUpperInvariant(),
        };

    private static string ResolveSourceLabel(LogEvent logEvent)
    {
        if (TryGetScalar(logEvent, "ShortSourceContext", out var shortName))
            return shortName + "()";

        if (!TryGetScalar(logEvent, "SourceContext", out var fullName))
            return "Application()";

        var lastDot = fullName.LastIndexOf('.');
        var className = lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
        return className + "()";
    }

    private static void WriteContextLine(TextWriter output, string label, LogEvent logEvent, string propertyName)
    {
        if (!TryGetScalar(logEvent, propertyName, out var value))
            return;

        if (string.IsNullOrWhiteSpace(value) || value == "-")
            return;

        output.WriteLine($"{label}: {value}");
    }

    private static bool TryGetScalar(LogEvent logEvent, string name, out string value)
    {
        value = "";
        if (!logEvent.Properties.TryGetValue(name, out var property))
            return false;

        if (property is not ScalarValue scalar || scalar.Value is null)
            return false;

        value = scalar.Value.ToString() ?? "";
        return true;
    }
}
