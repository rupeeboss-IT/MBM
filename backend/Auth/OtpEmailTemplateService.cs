using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;

namespace RB_Website_API.Auth;

public interface IOtpEmailTemplateService
{
    string BuildRegistrationOtpEmail(string otp);
}

public sealed class OtpEmailTemplateService : IOtpEmailTemplateService
{
    private const string RegistrationTemplateFile = "RegistrationOtpEmail.html";

    private readonly IWebHostEnvironment _env;
    private readonly ApplicationUrlsSettings _urls;
    private readonly ContactSettings _contact;
    private readonly ILogger<OtpEmailTemplateService> _logger;

    public OtpEmailTemplateService(
        IWebHostEnvironment env,
        IOptions<ApplicationUrlsSettings> urls,
        IOptions<ContactSettings> contact,
        ILogger<OtpEmailTemplateService> logger)
    {
        _env = env;
        _urls = urls.Value;
        _contact = contact.Value;
        _logger = logger;
    }

    public string BuildRegistrationOtpEmail(string otp)
    {
        var template = LoadTemplate(RegistrationTemplateFile);
        var registerUrl = string.IsNullOrWhiteSpace(_urls.FrontendBase)
            ? "#"
            : $"{_urls.FrontendBase}/register";

        return template
            .Replace("{{Otp}}", Encode(otp))
            .Replace("{{RegisterUrl}}", Encode(registerUrl))
            .Replace("{{SupportEmail}}", Encode(_contact.FromEmail))
            .Replace("{{SupportPhone}}", Encode(_contact.SupportPhone))
            .Replace("{{Year}}", DateTime.Now.Year.ToString(CultureInfo.InvariantCulture));
    }

    private string LoadTemplate(string fileName)
    {
        var path = Path.Combine(_env.ContentRootPath, "Templates", fileName);
        if (File.Exists(path))
            return File.ReadAllText(path, Encoding.UTF8);

        _logger.LogWarning("OTP email template not found at {Path}.", path);
        return """
               <p>Your MSME Bharat Manch verification code is <strong>{{Otp}}</strong>.</p>
               <p>This code expires in 10 minutes.</p>
               """;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value ?? "");
}
