using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Security;
using ReportTree.Server.DTOs;
using ReportTree.Server.Services;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.HttpOverrides;

namespace ReportTree.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
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
            builder.Services.AddSingleton<IAuditLogRepository, LiteDbAuditLogRepository>();
            builder.Services.AddSingleton<ILoginAttemptRepository, LiteDbLoginAttemptRepository>();
            
            // Services
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddScoped<AuditLogService>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<PageAuthorizationService>();
            builder.Services.AddSingleton<IPowerBIService, PowerBIService>();

            // Configure Security Policies from Configuration
            var passwordPolicy = new PasswordPolicy();
            builder.Configuration.Bind("Security:PasswordPolicy", passwordPolicy);
            builder.Services.AddSingleton(passwordPolicy);
            builder.Services.AddSingleton<PasswordValidator>();
            
            var rateLimitPolicy = new AppRateLimitPolicy();
            builder.Configuration.Bind("Security:RateLimitPolicy", rateLimitPolicy);
            
            var corsPolicy = new CorsPolicy();
            builder.Configuration.Bind("Security:CorsPolicy", corsPolicy);
            
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<PageAuthorizationService>();
            builder.Services.AddScoped<SettingsService>();
            builder.Services.AddScoped<AuditLogService>();
            
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

            // JWT Auth
            var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-super-secret-key-change-must-be-longer-than-256-bits";
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

            var app = builder.Build();
            
            // Use forwarded headers
            app.UseForwardedHeaders();
            
            // Add security headers middleware
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
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
                return token != null ? Results.Ok(new LoginResponse(token)) : Results.Unauthorized();
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

            // Serve the Vue.js frontend for all non-API routes
            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}

