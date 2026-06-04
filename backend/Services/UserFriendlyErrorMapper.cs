using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Exceptions;

namespace RB_Website_API.Services;

/// <summary>
/// Maps exceptions to safe, non-technical messages for API consumers. Never exposes stack traces, SQL, or EF details.
/// </summary>
public static partial class UserFriendlyErrorMapper
{
    public const string GenericMessage = "Something went wrong while processing your request.";
    public const string GenericLaterMessage = "Something went wrong. Please try again later.";
    public const string SupportMessage = "Something went wrong. Please contact support if the issue continues.";

    private static readonly string[] TechnicalIndicators =
    [
        "LINQ", "Entity Framework", "EF Core", "SqlException", "Microsoft.", "System.",
        "DbContext", "Repository", "Controller", " at ", ".cs", "line ", "stack",
        "inner exception", "connection string", "Could not translate", "Translation of",
        "Invalid column", "Invalid object name", "SqlServer", "DbUpdate",
        "Object reference not set", "NullReference", "IOException", "DirectoryNotFound",
        "Path:", ":\\", "Missing ", "not configured", "Jwt:", "Host is not set",
    ];

    public static string GetUserMessage(Exception exception, string? operationKey = null)
    {
        var ex = Unwrap(exception);

        if (ex is UserFacingException ufe && TrySanitize(ufe.Message, out var safeUfe))
            return safeUfe;

        if (ex is OtpRateLimitExceededException && TrySanitize(ex.Message, out var rateMsg))
            return rateMsg;

        if (ex is UnauthorizedAccessException)
            return "Your session is invalid. Please sign in again.";

        if ((ex is InvalidOperationException or ArgumentException) && TrySanitize(ex.Message, out var argMsg))
            return argMsg;

        return ex switch
        {
            DbUpdateException or DbException => GetSaveMessage(operationKey),
            TimeoutException => "The request is taking longer than expected. Please try again.",
            FileNotFoundException or DirectoryNotFoundException => "The requested file is not available.",
            IOException => "We could not access the file. Please try again.",
            HttpRequestException => "We could not complete the request to an external service. Please try again.",
            _ when IsDatabaseOrLinqException(ex) => GetLoadMessage(operationKey),
            _ => GetDefaultForOperation(operationKey),
        };
    }

    public static int GetStatusCode(Exception exception)
    {
        var ex = Unwrap(exception);
        return ex switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            OtpRateLimitExceededException => StatusCodes.Status429TooManyRequests,
            UserFacingException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            FileNotFoundException or DirectoryNotFoundException => StatusCodes.Status404NotFound,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            _ => StatusCodes.Status500InternalServerError,
        };
    }

    public static bool IsSafeUserMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        return TrySanitize(message, out _);
    }

    private static Exception Unwrap(Exception ex)
    {
        while (ex is AggregateException agg && agg.InnerException is not null)
            ex = agg.InnerException;
        return ex;
    }

    private static bool IsDatabaseOrLinqException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            var name = current.GetType().FullName ?? "";
            if (name.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase)
                || name.Contains("SqlClient", StringComparison.OrdinalIgnoreCase)
                || name.Contains("SqlException", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("LINQ", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("could not be translated", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool TrySanitize(string? message, out string sanitized)
    {
        sanitized = "";
        if (string.IsNullOrWhiteSpace(message)) return false;

        var text = message.Trim();
        if (text.Length > 280) return false;

        foreach (var indicator in TechnicalIndicators)
        {
            if (text.Contains(indicator, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (TechnicalPathPattern().IsMatch(text)) return false;

        sanitized = text;
        return true;
    }

    private static string GetDefaultForOperation(string? operationKey) => operationKey switch
    {
        "login" => "Unable to sign in. Please verify your credentials.",
        "register" => "Unable to complete registration. Please try again.",
        "forgot_password" => "Unable to process your password reset request. Please try again.",
        "reset_password" => "Unable to reset your password. Please try again.",
        "send_email_otp" or "send_sms_otp" => "Unable to send the verification code. Please try again.",
        "verify_email_otp" or "verify_sms_otp" => "Unable to verify the code. Please try again.",
        "report_history" => "Unable to load report history. Please try again later.",
        "report_upload" => "Unable to upload the report. Please try again.",
        "report_download" => "Unable to download the report. Please try again.",
        "report_search" => "Unable to retrieve search results. Please try again.",
        "payment_create" => "Unable to start payment. Please try again.",
        "payment_verify" => "Unable to verify payment. Please try again.",
        "invoice_list" => "Unable to load invoices. Please try again.",
        "invoice_download" => "Unable to download the invoice. Please try again.",
        "profile" => "Unable to load your profile. Please try again.",
        "my_plan" => "Unable to load your plan details. Please try again.",
        "dashboard" => "Unable to load dashboard data. Please try again.",
        "save" => "Unable to save changes. Please try again.",
        "delete" => "Unable to delete the record. Please try again.",
        "search" => "Unable to retrieve results. Please try again.",
        _ => GenericLaterMessage,
    };

    private static string GetLoadMessage(string? operationKey) =>
        operationKey?.Contains("upload", StringComparison.OrdinalIgnoreCase) == true
            ? "Unable to upload the report. Please try again."
            : operationKey?.Contains("download", StringComparison.OrdinalIgnoreCase) == true
                ? "Unable to download the file. Please try again."
                : GetDefaultForOperation(operationKey);

    private static string GetSaveMessage(string? operationKey) =>
        operationKey?.Contains("delete", StringComparison.OrdinalIgnoreCase) == true
            ? "Unable to delete the record. Please try again."
            : "Unable to save changes. Please try again.";

    [GeneratedRegex(@"[A-Za-z]:\\|/[\w./-]+\.(cs|dll|sql)", RegexOptions.IgnoreCase)]
    private static partial Regex TechnicalPathPattern();
}
