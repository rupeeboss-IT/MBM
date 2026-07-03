namespace RB_Website_API.Auth;

public static class PasswordResetMessages
{
    public const string AccountNotFoundReason = "account_not_found";

    public static string AccountNotFound(string channel) =>
        channel == "email" ? AccountNotFoundEmail : AccountNotFoundMobile;

    public const string AccountNotFoundEmail =
        "We could not find a member or partner account registered with this email address. " +
        "Please check for spelling mistakes, try another email you may have used when signing up, " +
        "or create a new account if you have not registered yet.";

    public const string AccountNotFoundMobile =
        "We could not find a member or partner account registered with this mobile number. " +
        "Please enter the same 10-digit mobile number you used during registration, " +
        "or create a new account if you have not signed up yet.";
}
