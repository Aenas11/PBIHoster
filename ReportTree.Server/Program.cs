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
            builder.Services.AddSingleton<IThemeRepository>(_ => new LiteDbThemeRepository(dbConnectionString));
            builder.Services.AddSingleton<IPageRepository, LiteDbPageRepository>();
            builder.Services.AddScoped<AuthService>();

            // JWT Auth
            var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-super-secret-key-change-must-be-longer-than-256-bits";
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ReportTree";
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

            builder.Services.AddSingleton<ITokenService>(new TokenService(jwtIssuer, signingKey));

            var app = builder.Build();

            app.UseDefaultFiles();
            app.MapStaticAssets();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
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

            // Serve the Vue.js frontend for all non-API routes
            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}

