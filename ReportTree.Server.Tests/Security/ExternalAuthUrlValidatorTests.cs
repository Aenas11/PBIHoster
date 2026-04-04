using ReportTree.Server.Security;
using Xunit;

namespace ReportTree.Server.Tests.Security;

public class ExternalAuthUrlValidatorTests
{
    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("   ", "/")]
    [InlineData("https://evil.example", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/dashboard", "/dashboard")]
    [InlineData("/dashboard?tab=reports", "/dashboard?tab=reports")]
    public void NormalizeReturnUrl_RejectsUnsafeAndAllowsLocal(string? input, string expected)
    {
        var normalized = ExternalAuthUrlValidator.NormalizeReturnUrl(input);
        Assert.Equal(expected, normalized);
    }
}