using ReportTree.Server.Models;

namespace ReportTree.Server.DTOs;

public class AuditExportQueryParameters : AuditLogQuery
{
    public string Format { get; set; } = "csv";
}