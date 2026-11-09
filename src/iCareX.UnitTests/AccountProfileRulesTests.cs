using iCareX.Domain.Common;
using iCareX.Domain.Exceptions;
using Xunit;

namespace iCareX.UnitTests;

public sealed class AccountProfileRulesTests
{
    [Theory]
    [InlineData("  Nguyễn Văn An  ", " 0912345678 ", "Nguyễn Văn An", "0912345678")]
    [InlineData("An", null, "An", "")]
    [InlineData("An", "   ", "An", "")]
    public void NormalizesProfileAndAllowsClearingPhone(string name, string? phone, string expectedName, string expectedPhone)
        => Assert.Equal((expectedName, expectedPhone), AccountProfileRules.Normalize(name, phone));

    [Theory]
    [InlineData(null, null)]
    [InlineData("  A  ", "")]
    [InlineData("Name\nInjected", "")]
    [InlineData("Valid name", "091 2345678")]
    [InlineData("Valid name", "+84912345678")]
    [InlineData("Valid name", "09123456789")]
    [InlineData("Valid name", "0１２３４５６７８９")]
    public void RejectsInvalidProfile(string? name, string? phone)
        => Assert.Throws<RuleViolation>(() => AccountProfileRules.Normalize(name, phone));

    [Fact]
    public void RejectsOverlongName()
        => Assert.Throws<RuleViolation>(() => AccountProfileRules.Normalize(new string('a', 61), ""));
}
