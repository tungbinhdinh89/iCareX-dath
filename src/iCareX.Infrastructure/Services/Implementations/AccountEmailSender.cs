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
    public int DeliveryTimeoutSeconds { get; set; } = 10;
    public string PublicBaseUrl { get; set; } = "";

    public string Link(string path, string fragment)
    {
        if (!Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)
            || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new EmailDeliveryUnavailableException("invalid_public_base_url");
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
            || options.DeliveryTimeoutSeconds is < 1 or > 60
            || !MailAddress.TryCreate(options.From, out _)) throw new EmailDeliveryUnavailableException("invalid_smtp_configuration");
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
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.DeliveryTimeoutSeconds));
        try { await smtp.SendMailAsync(message, timeout.Token); }
        catch (SmtpException) { throw new EmailDeliveryUnavailableException("smtp_send_failed"); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new EmailDeliveryUnavailableException("smtp_timeout"); }
    }
}
