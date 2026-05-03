namespace RB_Website_API.Auth;

public record SendEmailOtpRequest(string Email);
public record VerifyEmailOtpRequest(string Email, string Code);
public record SendSmsOtpRequest(string Phone);
public record VerifySmsOtpRequest(string Phone, string Code);

public record ApiOk(bool Success, string? Message = null);

