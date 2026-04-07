using System.ComponentModel.DataAnnotations;

namespace ReportTree.Server.DTOs;

public record CommentResponseDto(
    int Id,
    int PageId,
    int? ParentId,
    string Username,
    string Content,
    List<string> Mentions,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateCommentRequest(
    [Required, MinLength(1), MaxLength(4000)] string Content,
    int? ParentId,
    List<string>? Mentions
);

public record UpdateCommentRequest(
    [Required, MinLength(1), MaxLength(4000)] string Content,
    List<string>? Mentions
);