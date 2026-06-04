namespace RB_Website_API.Exceptions;

/// <summary>
/// Explicit business error safe to return to clients. Message must still be plain language.
/// </summary>
public sealed class UserFacingException : Exception
{
    public UserFacingException(string message) : base(message) { }
}
