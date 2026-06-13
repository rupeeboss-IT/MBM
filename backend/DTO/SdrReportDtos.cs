namespace RB_Website_API.DTO;

public sealed record SdrGenerationResult(
    bool Success,
    string Outcome,
    string UserMessage,
    Guid? ReportId,
    DateTime? ExpiryDate,
    string? RequestStatus);

public sealed record SaarthiSdrApiResult(
    bool Success,
    byte[]? PdfBytes,
    string? ErrorMessage,
    string? ExternalReference);
