
namespace ReportTree.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddEndpointsApiExplorer();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // LiteDB
            builder.Services.AddSingleton<LiteDB.LiteDatabase>(_ => new LiteDB.LiteDatabase(builder.Configuration["LiteDb:ConnectionString"] ?? "Filename=reporttree.db;Connection=shared"));
            builder.Services.AddSingleton<IUserRepository, LiteDbUserRepository>();

            // JWT Auth
            var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-super-secret-key-change";
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
            app.MapPost("/api/auth/register", (RegisterRequest req, IUserRepository repo) =>
            {
                var user = new AppUser { Username = req.Username, Roles = req.Roles?.ToList() ?? new List<string>() };
                repo.Upsert(user, req.Password);
                return Results.Ok();
            });

            app.MapPost("/api/auth/login", (LoginRequest req, IUserRepository repo, ITokenService tokens) =>
            {
                var user = repo.Validate(req.Username, req.Password);
                if (user == null) return Results.Unauthorized();
                var token = tokens.Generate(user);
                return Results.Ok(new { token });
            });

            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}

// RBAC models and services
public record RegisterRequest(string Username, string Password, IEnumerable<string>? Roles);
public record LoginRequest(string Username, string Password);

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public interface IUserRepository
{
    void Upsert(AppUser user, string plainPassword);
    AppUser? Validate(string username, string plainPassword);
}

public class LiteDbUserRepository : IUserRepository
{
    private readonly LiteDB.LiteDatabase _db;
    public LiteDbUserRepository(LiteDB.LiteDatabase db) { _db = db; }

    public void Upsert(AppUser user, string plainPassword)
    {
        var col = _db.GetCollection<AppUser>("users");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        col.Upsert(user);
    }

    public AppUser? Validate(string username, string plainPassword)
    {
        var col = _db.GetCollection<AppUser>("users");
        var user = col.FindOne(x => x.Username == username);
        if (user == null) return null;
        return BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash) ? user : null;
    }
}

public interface ITokenService
{
    string Generate(AppUser user);
}

public class TokenService : ITokenService
{
    private readonly string _issuer;
    private readonly Microsoft.IdentityModel.Tokens.SymmetricSecurityKey _key;
    public TokenService(string issuer, Microsoft.IdentityModel.Tokens.SymmetricSecurityKey key) { _issuer = issuer; _key = key; }

    public string Generate(AppUser user)
    {
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(_key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, user.Username)
        };
        claims.AddRange(user.Roles.Select(r => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r)));

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _issuer,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );
        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
