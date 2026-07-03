namespace RB_Website_API.Auth;

/// <summary>
/// SMS bodies for OTP delivery. Registration and password reset share the same gateway-approved template.
/// </summary>
public static class SmsOtpMessages
{
    public static string VerificationCode(string otp) =>
        $"Dear Customer,\nYour mobile verification code is {otp}\nPlease use this code to verify your account. Regard Team RupeeBoss.";
}
