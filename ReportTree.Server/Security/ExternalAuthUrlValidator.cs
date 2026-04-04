namespace ReportTree.Server.Security;

public static class ExternalAuthUrlValidator
{
    public static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        returnUrl = returnUrl.Trim();

        if (!returnUrl.StartsWith('/'))
        {
            return "/";
        }

        // Prevent protocol-relative redirects (e.g. //evil.example)
        if (returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return "/";
        }

        return returnUrl;
    }
}