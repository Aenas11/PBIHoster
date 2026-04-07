namespace ReportTree.Server.DTOs;

public record PageVersionDto(
    int Id,
    int PageId,
    string Layout,
    string ChangedBy,
    DateTime ChangedAt,
    string ChangeDescription
);

public record PageRollbackRequest(string? ChangeDescription);
