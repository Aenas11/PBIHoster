using ReportTree.Server.Models;
using System.Security.Claims;

namespace ReportTree.Server.Services;

public class PageAuthorizationService
{
    /// <summary>
    /// Determines if a user can access a specific page based on their authentication status,
    /// roles, groups, and the page's access control settings.
    /// </summary>
    public bool CanAccessPage(Page page, ClaimsPrincipal user)
    {
        // Public pages are accessible to everyone
        if (page.IsPublic)
        {
            return true;
        }

        // Non-public pages require authentication
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Extract user claims
        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var userGroups = user.Claims
            .Where(c => c.Type == "Group")
            .Select(c => c.Value)
            .ToList();

        var username = user.Identity?.Name;

        // Admins can access everything
        if (userRoles.Contains("Admin"))
        {
            return true;
        }

        // Check if user is explicitly allowed
        if (username != null && (page.AllowedUsers ?? new List<string>()).Contains(username))
        {
            return true;
        }

        // Check if user is in an allowed group
        if ((page.AllowedGroups ?? new List<string>()).Any(g => userGroups.Contains(g)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Filters a list of pages to only those the user can access.
    /// </summary>
    public IEnumerable<Page> FilterAccessiblePages(IEnumerable<Page> pages, ClaimsPrincipal user)
    {
        return pages.Where(p => CanAccessPage(p, user));
    }
}
