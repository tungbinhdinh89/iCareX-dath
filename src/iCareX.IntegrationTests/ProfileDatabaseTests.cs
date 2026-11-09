using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using iCareX.Shared.Contracts.Identity;
using Xunit;

namespace iCareX.IntegrationTests;

public sealed partial class DatabaseTests
{
    [Fact]
    public async Task ProfileUpdatePersistsOnlyOwnNameAndPhoneAndKeepsIdentityAndSession()
    {
        using var host = SecurityHost(new CapturedEmail());
        using var client = await SecurityClient(host);
        var email = $"{Guid.NewGuid():N}@example.test";
        var otherEmail = $"{Guid.NewGuid():N}@example.test";
        const string password = "TestPassword@123";
        var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new { name = "Original profile", email, password, phone = "" });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var actor = (await registered.Content.ReadFromJsonAsync<RegistrationResponse>())!;
        var otherCreated = await client.PostAsJsonAsync("/api/v1/auth/register", new { name = "Other profile", email = otherEmail, password, phone = "" });
        var other = (await otherCreated.Content.ReadFromJsonAsync<RegistrationResponse>())!;
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/auth/me", new { name = " A ", phone = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/auth/me", new { name = "Valid name", phone = "invalid" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/auth/me", new { name = "Forged", phone = "", userId = other.Id })).StatusCode);
        var saved = await client.PutAsJsonAsync("/api/v1/auth/me", new { name = "  Nguyễn Văn An  ", phone = " 0912345678 " });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var me = (await client.GetFromJsonAsync<AccountResponse>("/api/v1/auth/me"))!;
        Assert.Equal("Nguyễn Văn An", me.Name);
        Assert.Equal("0912345678", me.Phone);
        Assert.Equal(email, me.Email);
        Assert.False(me.EmailVerified);
        Assert.Equal(actor.Roles, me.Roles);
        await using (var db = Context())
        {
            Assert.Equal(me.Name, (await db.Accounts.SingleAsync(x => x.Id == actor.Id)).Name);
            Assert.Equal("Other profile", (await db.Accounts.SingleAsync(x => x.Id == other.Id)).Name);
            Assert.False((await db.Users.SingleAsync(x => x.Id == actor.Id)).EmailConfirmed);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/v1/auth/me", new { name = me.Name, phone = (string?)null })).StatusCode);
        Assert.Equal("", (await client.GetFromJsonAsync<AccountResponse>("/api/v1/auth/me"))!.Phone);
    }
}
