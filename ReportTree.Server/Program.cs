using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using System.Diagnostics;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Security;
using ReportTree.Server.DTOs;
using ReportTree.Server.Services;
using ReportTree.Server.HealthChecks;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

namespace ReportTree.Server
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var keyVaultUri = builder.Configuration["KEY_VAULT_URI"] ?? builder.Configuration["AZURE_KEY_VAULT_URI"];
            if (!string.IsNullOrWhiteSpace(keyVaultUri))
            {
                builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
            }

            SecretValidator.Validate(builder.Configuration);

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithProcessId()
                    .Enrich.WithThreadId()
                    .WriteTo.Console(new RenderedCompactJsonFormatter());
            });
            
            // Configure Kestrel to listen on port from environment or default to 8080
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                var port = builder.Configuration.GetValue<int>("PORT", 8080);
                serverOptions.ListenAnyIP(port);
            });
            
            builder.Services.AddEndpointsApiExplorer();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // LiteDB
            var dbConnectionString = builder.Configuration["LiteDb:ConnectionString"] ?? "Filename=reporttree.db;Connection=shared";
            builder.Services.AddSingleton<LiteDB.LiteDatabase>(_ => new LiteDB.LiteDatabase(dbConnectionString));
            builder.Services.AddSingleton<IUserRepository, LiteDbUserRepository>();
            builder.Services.AddSingleton<IGroupRepository, LiteDbGroupRepository>();
            builder.Services.AddSingleton<IThemeRepository, LiteDbThemeRepository>();
            builder.Services.AddSingleton<IPageRepository, LiteDbPageRepository>();
            builder.Services.AddSingleton<ISettingsRepository, LiteDbSettingsRepository>();
            builder.Services.AddSingleton<IBrandingAssetRepository, LiteDbBrandingAssetRepository>();
            builder.Services.AddSingleton<IAuditLogRepository, LiteDbAuditLogRepository>();
            builder.Services.AddSingleton<ILoginAttemptRepository, LiteDbLoginAttemptRepository>();
            builder.Services.AddSingleton<IDatasetRefreshScheduleRepository, LiteDbDatasetRefreshScheduleRepository>();
            builder.Services.AddSingleton<IDatasetRefreshRunRepository, LiteDbDatasetRefreshRunRepository>();
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" })
                .AddCheck<LiteDbHealthCheck>("database", tags: new[] { "ready" });
            
            // Services
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<ISettingsService>(sp => sp.GetRequiredService<SettingsService>());
            builder.Services.AddSingleton<BrandingService>();
            builder.Services.AddScoped<AuditLogService>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<PageAuthorizationService>();
            builder.Services.AddSingleton<IPowerBIService, PowerBIService>();
            builder.Services.AddSingleton<DemoContentService>();
            builder.Services.AddSingleton<RefreshNotificationService>();
            builder.Services.AddScoped<DatasetRefreshService>();
            builder.Services.Configure<RefreshOptions>(builder.Configuration.GetSection("Refresh"));
            builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
            builder.Services.AddSingleton<EmailService>();
            builder.Services.AddHostedService<RefreshSchedulerHostedService>();

            // Configure Security Policies from Configuration
            var passwordPolicy = new PasswordPolicy();
            builder.Configuration.Bind("Security:PasswordPolicy", passwordPolicy);
            builder.Services.AddSingleton(passwordPolicy);
            builder.Services.AddSingleton<PasswordValidator>();
            
            var rateLimitPolicy = new AppRateLimitPolicy();
            builder.Configuration.Bind("Security:RateLimitPolicy", rateLimitPolicy);
            
            var corsPolicy = new CorsPolicy();
            builder.Configuration.Bind("Security:CorsPolicy", corsPolicy);
            
            var contentSecurityPolicy = new ContentSecurityPolicy();
            builder.Configuration.Bind("Security:ContentSecurityPolicy", contentSecurityPolicy);

            builder.Services.AddSingleton(contentSecurityPolicy);
            builder.Services.AddScoped<IPowerBIDiagnosticsService, PowerBIDiagnosticsService>();
            
            // Add HttpContextAccessor for audit logging
            builder.Services.AddHttpContextAccessor();
            
            // Configure rate limiting
            if (rateLimitPolicy.Enabled)
            {
                builder.Services.AddMemoryCache();
                builder.Services.Configure<IpRateLimitOptions>(options =>
                {
                    options.EnableEndpointRateLimiting = true;
                    options.StackBlockedRequests = false;
                    options.HttpStatusCode = 429;
                    options.RealIpHeader = "X-Real-IP";
                    options.GeneralRules = new List<RateLimitRule>
                    {
                        new RateLimitRule
                        {
                            Endpoint = "*",
                            Period = rateLimitPolicy.GeneralPeriod,
                            Limit = rateLimitPolicy.GeneralLimit
                        },
                        new RateLimitRule
                        {
                            Endpoint = "*/api/auth/*",
                            Period = rateLimitPolicy.AuthPeriod,
                            Limit = rateLimitPolicy.AuthLimit
                        }
                    };
                });
                builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
                builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
                builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
                builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            }
            
            // Configure CORS
            if (corsPolicy.AllowedOrigins?.Any() == true)
            {
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("CorsPolicy", policy =>
                    {
                        policy.WithOrigins(corsPolicy.AllowedOrigins.ToArray())
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                        
                        if (corsPolicy.AllowCredentials)
                        {
                            policy.AllowCredentials();
                        }
                    });
                });
            }
            
            // Add memory cache for performance
            builder.Services.AddMemoryCache();
            builder.Services.AddResponseCaching();
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });
            builder.Services.AddHttpClient();

            // JWT Auth
            var jwtKey = builder.Configuration["Jwt:Key"]!;
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ReportTree";
            var jwtExpiryHours = builder.Configuration.GetValue<int>("Jwt:ExpiryHours", 8);
            var signingKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey));

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidIssuer = jwtIssuer,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("CanViewReports", policy => policy.RequireRole("Admin", "Editor", "Viewer"));
                options.AddPolicy("CanManageReports", policy => policy.RequireRole("Admin", "Editor"));
                options.AddPolicy("CanManageUsers", policy => policy.RequireRole("Admin"));
            });

            builder.Services.AddSingleton<ITokenService>(sp =>
            {
                var groupRepo = sp.GetRequiredService<IGroupRepository>();
                return new TokenService(jwtIssuer, signingKey, groupRepo, jwtExpiryHours);
            });
            
            // Configure forwarded headers for reverse proxy (Caddy)
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // Observability
            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ReportTree.Server"))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddPrometheusExporter();
                });

            var app = builder.Build();
            
            // Use forwarded headers
            app.UseForwardedHeaders();

            // Apply correlation IDs early in the pipeline
            app.Use(async (context, next) =>
            {
                const string correlationHeader = "X-Correlation-ID";
                var correlationId = context.Request.Headers.TryGetValue(correlationHeader, out var existingId) &&
                                    !string.IsNullOrWhiteSpace(existingId)
                    ? existingId.ToString()
                    : Guid.NewGuid().ToString();

                context.TraceIdentifier = correlationId;
                context.Response.Headers[correlationHeader] = correlationId;

                var activity = Activity.Current;
                var started = false;
                if (activity == null)
                {
                    activity = new Activity("Request");
                    activity.SetIdFormat(ActivityIdFormat.W3C);
                    activity.Start();
                    started = true;
                }

                activity?.SetTag("correlation_id", correlationId);

                using (LogContext.PushProperty("CorrelationId", correlationId))
                {
                    try
                    {
                        await next();
                    }
                    finally
                    {
                        if (started)
                        {
                            activity?.Stop();
                        }
                    }
                }
            });

            app.UseSerilogRequestLogging();
            
            // Add security headers middleware
            var cspHeaderValue = BuildContentSecurityPolicy(contentSecurityPolicy, corsPolicy);
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
                context.Response.Headers.Append("Content-Security-Policy", cspHeaderValue);
                await next();
            });

            app.UseDefaultFiles();
            app.MapStaticAssets();
            
            // Use rate limiting
            if (rateLimitPolicy.Enabled)
            {
                app.UseIpRateLimiting();
            }
            
            // Use CORS
            if (corsPolicy.AllowedOrigins?.Any() == true)
            {
                app.UseCors("CorsPolicy");
            }
            
            // Add response caching and compression
            app.UseResponseCaching();
            app.UseResponseCompression();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            else
            {
                // Global error handling for production
                app.UseExceptionHandler("/error");
            }

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live")
            });
            app.MapHealthChecks("/ready", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready")
            });
            app.MapPrometheusScrapingEndpoint();

            // app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            // Minimal auth endpoints
            app.MapPost("/api/auth/register", async (RegisterRequest req, AuthService auth) =>
            {
                var (success, errors) = await auth.RegisterAsync(req.Username, req.Password, req.Roles?.ToList() ?? new List<string>());
                return success ? Results.Ok() : Results.BadRequest(new { Errors = errors });
            });

            app.MapPost("/api/auth/login", async (LoginRequest req, AuthService auth) =>
            {
                var (token, errorMessage) = await auth.LoginAsync(req.Username, req.Password);
                return token != null ? Results.Ok(new LoginResponse(token)) : Results.BadRequest(new { Error = errorMessage });
            });
            
            // Global error handler
            app.Map("/error", (HttpContext context) =>
            {
                var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                var isDevelopment = app.Environment.IsDevelopment();
                
                return Results.Problem(
                    title: "An error occurred",
                    detail: isDevelopment ? exception?.Error.Message : "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            });

            // Initialize default settings
            using (var scope = app.Services.CreateScope())
            {
                var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
                settingsService.InitializeDefaultSettingsAsync().Wait();
            }

            app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
            
            // Serve the Vue.js frontend for all non-API routes
            app.MapFallbackToFile("/index.html");

            app.Run();
        }

        private static string BuildContentSecurityPolicy(ContentSecurityPolicy policy, CorsPolicy corsPolicy)
        {
            static string JoinSources(IEnumerable<string> sources) => string.Join(' ', sources.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());

            var frameSources = policy.FrameSources.AsEnumerable();
            if (corsPolicy.AllowedOrigins?.Any() == true)
            {
                frameSources = frameSources.Concat(corsPolicy.AllowedOrigins);
            }

            var directives = new[]
            {
                $"default-src {JoinSources(policy.DefaultSources)}",
                $"frame-src {JoinSources(frameSources)}",
                $"frame-ancestors {JoinSources(policy.FrameAncestors)}",
                $"script-src {JoinSources(policy.ScriptSources)}",
                $"style-src {JoinSources(policy.StyleSources)}",
                $"img-src {JoinSources(policy.ImgSources)}",
                $"connect-src {JoinSources(policy.ConnectSources)}",
                $"font-src {JoinSources(policy.FontSources)}",
                "object-src 'none'"
            };

            return string.Join("; ", directives.Where(d => !string.IsNullOrWhiteSpace(d)));
        }
    }
}
