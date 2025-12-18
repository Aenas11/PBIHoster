using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Security;
using ReportTree.Server.DTOs;
using ReportTree.Server.Services;

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
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<PageAuthorizationService>();
            builder.Services.AddScoped<SettingsService>();
            builder.Services.AddScoped<AuditLogService>();
            
            // Add HttpContextAccessor for audit logging
            builder.Services.AddHttpContextAccessor();
            
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

            var app = builder.Build();

            app.UseDefaultFiles();
            app.MapStaticAssets();
            
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
                await auth.RegisterAsync(req.Username, req.Password, req.Roles?.ToList() ?? new List<string>());
                return Results.Ok();
            });

            app.MapPost("/api/auth/login", async (LoginRequest req, AuthService auth) =>
            {
                var token = await auth.LoginAsync(req.Username, req.Password);
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

            // Serve the Vue.js frontend for all non-API routes
            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}

