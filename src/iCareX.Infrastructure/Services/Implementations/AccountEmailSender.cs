using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using iCareX.Application.Features.Identity.Contracts;

namespace iCareX.Infrastructure.Services.Implementations;

public sealed class AccountEmailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
    public string PublicBaseUrl { get; set; } = "";

    public string Link(string path, string fragment)
    {
        if (!Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)
            || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new EmailDeliveryUnavailableException();
        return $"{uri.AbsoluteUri.TrimEnd('/')}/{path.TrimStart('/')}#{fragment}";
    }
}

public sealed class AccountEmailSender(IOptions<AccountEmailOptions> settings) : IAccountEmailSender
{
    public void EnsureConfigured()
    {
        var options = settings.Value;
        _ = options.Link("", "");
        if (string.IsNullOrWhiteSpace(options.Host) || options.Port is < 1 or > 65535
            || !MailAddress.TryCreate(options.From, out _)) throw new EmailDeliveryUnavailableException();
    }

    public async Task SendAsync(string recipient, string subject, string text, CancellationToken ct)
    {
        EnsureConfigured();
        var options = settings.Value;
        using var smtp = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrEmpty(options.UserName) ? null : new NetworkCredential(options.UserName, options.Password)
        };
        using var message = new MailMessage(options.From, recipient, subject, text);
        try { await smtp.SendMailAsync(message, ct); }
        catch (SmtpException) { throw new EmailDeliveryUnavailableException(); }
    }
}
