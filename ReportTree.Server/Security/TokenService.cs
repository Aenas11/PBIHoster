using ReportTree.Server.Models;

namespace ReportTree.Server.Security
{
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
            claims.AddRange(user.Groups.Select(g => new System.Security.Claims.Claim("Group", g)));

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

}
