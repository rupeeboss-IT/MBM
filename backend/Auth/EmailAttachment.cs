namespace RB_Website_API.Auth;

public sealed record EmailAttachment(string FileName, byte[] Content, string ContentType = "application/octet-stream");
