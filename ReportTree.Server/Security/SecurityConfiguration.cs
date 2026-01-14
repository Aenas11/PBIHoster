namespace ReportTree.Server.Security;

public class SecurityConfiguration
{
    public PasswordPolicy PasswordPolicy { get; set; } = new();
    public AppRateLimitPolicy AppRateLimitPolicy { get; set; } = new();
    public SessionPolicy SessionPolicy { get; set; } = new();
    public CorsPolicy CorsPolicy { get; set; } = new();
    public ContentSecurityPolicy ContentSecurityPolicy { get; set; } = new();
}

public class PasswordPolicy
{
    public int MinLength { get; set; } = 8;
    public int MaxLength { get; set; } = 128;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialChar { get; set; } = true;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}

public class AppRateLimitPolicy
{
    public bool Enabled { get; set; } = true;
    public int GeneralLimit { get; set; } = 100;
    public string GeneralPeriod { get; set; } = "1m";
    public int AuthLimit { get; set; } = 5;
    public string AuthPeriod { get; set; } = "1m";
}

public class SessionPolicy
{
    public bool EnableConcurrentSessions { get; set; } = true;
    public int MaxConcurrentSessions { get; set; } = 3;
    public int SessionTimeoutMinutes { get; set; } = 30;
}

public class CorsPolicy
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public bool AllowCredentials { get; set; } = true;
}

public class ContentSecurityPolicy
{
    public string[] DefaultSources { get; set; } = new[] { "'self'" };
    public string[] FrameSources { get; set; } = new[] { "'self'", "https://app.powerbi.com", "https://*.powerbi.com" };
    public string[] ScriptSources { get; set; } = new[] { "'self'", "https://js.powerbi.com" };
    public string[] StyleSources { get; set; } = new[] { "'self'", "'unsafe-inline'", "https://fonts.googleapis.com" };
    public string[] ImgSources { get; set; } = new[] { "'self'", "data:", "https://*.powerbi.com" };
    public string[] ConnectSources { get; set; } = new[] { "'self'", "https://api.powerbi.com", "https://*.analysis.windows.net" };
    public string[] FontSources { get; set; } = new[] { "'self'", "data:" };
    public string[] FrameAncestors { get; set; } = new[] { "'self'" };
}
