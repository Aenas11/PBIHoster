using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportTree.Server.DTOs;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Services;

namespace ReportTree.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private static readonly Regex MentionRegex = new("@([A-Za-z0-9._-]+)", RegexOptions.Compiled);

    private readonly ICommentRepository _commentRepo;
    private readonly IPageRepository _pageRepo;
    private readonly PageAuthorizationService _pageAuthorization;
    private readonly AuditLogService _auditLogService;
    private readonly SettingsService _settingsService;

    public CommentsController(
        ICommentRepository commentRepo,
        IPageRepository pageRepo,
        PageAuthorizationService pageAuthorization,
        AuditLogService auditLogService,
        SettingsService settingsService)
    {
        _commentRepo = commentRepo;
        _pageRepo = pageRepo;
        _pageAuthorization = pageAuthorization;
        _auditLogService = auditLogService;
        _settingsService = settingsService;
    }

    [HttpGet("page/{pageId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByPage(int pageId)
    {
        if (!await _settingsService.IsCommentsEnabledAsync())
        {
            return NotFound();
        }

        var page = await _pageRepo.GetByIdAsync(pageId);
        if (page == null)
        {
            return NotFound();
        }

        if (!_pageAuthorization.CanAccessPage(page, User))
        {
            return User.Identity?.IsAuthenticated == true ? Forbid() : Unauthorized();
        }

        var comments = await _commentRepo.GetByPageIdAsync(pageId);
        var response = comments.Select(ToResponse).ToList();
        return Ok(response);
    }

    [HttpPost("page/{pageId:int}")]
    [Authorize]
    public async Task<IActionResult> Create(int pageId, [FromBody] CreateCommentRequest request)
    {
        if (!await _settingsService.IsCommentsEnabledAsync())
        {
            return NotFound();
        }

        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        var page = await _pageRepo.GetByIdAsync(pageId);
        if (page == null)
        {
            return NotFound();
        }

        if (!_pageAuthorization.CanAccessPage(page, User))
        {
            return Forbid();
        }

        if (request.ParentId.HasValue)
        {
            var parent = await _commentRepo.GetByIdAsync(request.ParentId.Value);
            if (parent == null || parent.PageId != pageId)
            {
                return BadRequest(new { Error = "Parent comment not found on this page" });
            }
        }

        var mentions = NormalizeMentions(request.Content, request.Mentions);

        var comment = new Comment
        {
            PageId = pageId,
            ParentId = request.ParentId,
            Username = username,
            Content = request.Content.Trim(),
            Mentions = mentions
        };

        var id = await _commentRepo.CreateAsync(comment);
        comment.Id = id;

        await _auditLogService.LogAsync("CREATE_COMMENT", $"Comment:{id}", $"Page={pageId}");

        return Ok(ToResponse(comment));
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCommentRequest request)
    {
        if (!await _settingsService.IsCommentsEnabledAsync())
        {
            return NotFound();
        }

        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        var existing = await _commentRepo.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var page = await _pageRepo.GetByIdAsync(existing.PageId);
        if (page == null)
        {
            return NotFound();
        }

        if (!_pageAuthorization.CanAccessPage(page, User))
        {
            return Forbid();
        }

        if (!CanEdit(existing, username))
        {
            return Forbid();
        }

        existing.Content = request.Content.Trim();
        existing.Mentions = NormalizeMentions(request.Content, request.Mentions);
        await _commentRepo.UpdateAsync(existing);

        await _auditLogService.LogAsync("UPDATE_COMMENT", $"Comment:{id}", $"Page={existing.PageId}");

        return Ok(ToResponse(existing));
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _settingsService.IsCommentsEnabledAsync())
        {
            return NotFound();
        }

        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        var existing = await _commentRepo.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var page = await _pageRepo.GetByIdAsync(existing.PageId);
        if (page == null)
        {
            return NotFound();
        }

        if (!_pageAuthorization.CanAccessPage(page, User))
        {
            return Forbid();
        }

        if (!CanDelete(existing, username))
        {
            return Forbid();
        }

        await _commentRepo.DeleteAsync(id);
        await _auditLogService.LogAsync("DELETE_COMMENT", $"Comment:{id}", $"Page={existing.PageId}");

        return Ok(new { deleted = true });
    }

    private bool CanEdit(Comment comment, string username)
    {
        return string.Equals(comment.Username, username, StringComparison.OrdinalIgnoreCase);
    }

    private bool CanDelete(Comment comment, string username)
    {
        return string.Equals(comment.Username, username, StringComparison.OrdinalIgnoreCase)
               || User.IsInRole("Admin");
    }

    private static List<string> NormalizeMentions(string content, List<string>? mentions)
    {
        var fromPayload = (mentions ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        var fromContent = MentionRegex.Matches(content)
            .Select(m => m.Groups[1].Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim());

        return fromPayload
            .Concat(fromContent)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
    }

    private static CommentResponseDto ToResponse(Comment comment) =>
        new(
            comment.Id,
            comment.PageId,
            comment.ParentId,
            comment.Username,
            comment.Content,
            comment.Mentions,
            comment.CreatedAt,
            comment.UpdatedAt
        );
}
