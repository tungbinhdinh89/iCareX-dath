using Microsoft.Extensions.Options;
using iCareX.Application.Features.Identity.Contracts;
using iCareX.Infrastructure.Services.Implementations;
using Xunit;

namespace iCareX.IntegrationTests;

[Trait("Category", "Host")]
public sealed class EmailConfigurationTests
{
    [Fact]
    public void MissingConfigurationReturnsSafeReasonWithoutCredentials()
    {
        var sender = new AccountEmailSender(Options.Create(new AccountEmailOptions { Password = "not-for-error-output" }));
        var error = Assert.Throws<EmailDeliveryUnavailableException>(sender.EnsureConfigured);
        Assert.Equal("invalid_public_base_url", error.Reason);
        Assert.DoesNotContain("not-for-error-output", error.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void DeliveryDeadlineMustBeBounded(int seconds)
    {
        var options = new AccountEmailOptions { PublicBaseUrl = "http://localhost:8443", Host = "localhost",
            From = "sender@example.test", DeliveryTimeoutSeconds = seconds };
        var sender = new AccountEmailSender(Options.Create(options));
        Assert.Equal("invalid_smtp_configuration", Assert.Throws<EmailDeliveryUnavailableException>(sender.EnsureConfigured).Reason);
    }
}
