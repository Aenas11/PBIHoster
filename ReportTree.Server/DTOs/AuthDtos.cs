using System.ComponentModel.DataAnnotations;

namespace ReportTree.Server.DTOs;

public record RegisterRequest(
    [Required, MinLength(3, ErrorMessage = "Username must be at least 3 characters long")]
    string Username,
    
    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    string Password,
    
    IEnumerable<string>? Roles
);

public record LoginRequest(
    [Required]
    string Username,
    
    [Required]
    string Password
);

public record LoginResponse(string Token);
