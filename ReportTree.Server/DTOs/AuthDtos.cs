namespace ReportTree.Server.DTOs;

public record RegisterRequest(string Username, string Password, IEnumerable<string>? Roles);
public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token);
