using ReportTree.Server.Models;
using ReportTree.Server.Persistance;

namespace ReportTree.Server.Security
{
    public class TokenService : ITokenService
    {
        private readonly string _issuer;
        private readonly Microsoft.IdentityModel.Tokens.SymmetricSecurityKey _key;
        private readonly int _expiryHours;
        private readonly IGroupRepository _groupRepo;
        
        public TokenService(string issuer, Microsoft.IdentityModel.Tokens.SymmetricSecurityKey key, IGroupRepository groupRepo, int expiryHours = 8) 
        { 
            _issuer = issuer; 
            _key = key;
            _groupRepo = groupRepo;
            _expiryHours = expiryHours;
        }

        public string Generate(AppUser user)
        {
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(_key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, user.Username)
        };
            claims.AddRange(user.Roles.Select(r => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r)));
            
            // Get groups from Group.Members (single source of truth)
            var userGroups = _groupRepo.GetAllAsync().Result
                .Where(g => g.Members.Contains(user.Username))
                .Select(g => g.Name);
            claims.AddRange(userGroups.Select(g => new System.Security.Claims.Claim("Group", g)));

            var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: _issuer,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_expiryHours),
                signingCredentials: creds
            );
            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }

}
