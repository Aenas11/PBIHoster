using System.Text.RegularExpressions;

namespace ReportTree.Server.Security;

public class PasswordValidator
{
    private readonly PasswordPolicy _policy;

    public PasswordValidator(PasswordPolicy policy)
    {
        _policy = policy;
    }

    public (bool IsValid, List<string> Errors) Validate(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required");
            return (false, errors);
        }

        if (password.Length < _policy.MinLength)
        {
            errors.Add($"Password must be at least {_policy.MinLength} characters long");
        }

        if (password.Length > _policy.MaxLength)
        {
            errors.Add($"Password must not exceed {_policy.MaxLength} characters");
        }

        if (_policy.RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        if (_policy.RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        if (_policy.RequireDigit && !Regex.IsMatch(password, @"\d"))
        {
            errors.Add("Password must contain at least one digit");
        }

        if (_policy.RequireSpecialChar && !Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
        {
            errors.Add("Password must contain at least one special character");
        }

        return (errors.Count == 0, errors);
    }
}
