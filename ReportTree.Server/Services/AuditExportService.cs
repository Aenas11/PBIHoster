using System.Text;
using ReportTree.Server.Models;

namespace ReportTree.Server.Services;

public sealed record AuditExportFile(byte[] Content, string ContentType, string FileName);

public class AuditExportService
{
    public AuditExportFile CreateExport(IReadOnlyCollection<AuditLog> logs, AuditLogQuery query, string format)
    {
        return format.Trim().ToLowerInvariant() switch
        {
            "csv" => CreateCsv(logs),
            "pdf" => CreatePdf(logs, query),
            _ => throw new ArgumentOutOfRangeException(nameof(format), "Supported formats are csv and pdf.")
        };
    }

    private static AuditExportFile CreateCsv(IEnumerable<AuditLog> logs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Username,Action,Resource,Success,IpAddress,UserAgent,Details");

        foreach (var log in logs)
        {
            sb.AppendLine(string.Join(',',
                Quote(log.Timestamp.ToString("O")),
                Quote(log.Username),
                Quote(log.Action),
                Quote(log.Resource),
                Quote(log.Success ? "true" : "false"),
                Quote(log.IpAddress),
                Quote(log.UserAgent),
                Quote(log.Details)));
        }

        return new AuditExportFile(
            Encoding.UTF8.GetBytes(sb.ToString()),
            "text/csv",
            $"audit-export-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private static AuditExportFile CreatePdf(IReadOnlyCollection<AuditLog> logs, AuditLogQuery query)
    {
        var lines = BuildPdfLines(logs, query);
        var pages = Paginate(lines, 48);
        var pdf = BuildPdfDocument(pages);

        return new AuditExportFile(
            pdf,
            "application/pdf",
            $"audit-export-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }

    private static List<string> BuildPdfLines(IEnumerable<AuditLog> logs, AuditLogQuery query)
    {
        var lines = new List<string>
        {
            "Compliance Audit Export",
            $"Generated (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            $"Filters: {BuildFilterSummary(query)}",
            string.Empty,
            "Timestamp            User              Action                 Success  Resource",
            "-------------------------------------------------------------------------------"
        };

        foreach (var log in logs)
        {
            lines.Add(
                $"{log.Timestamp:yyyy-MM-dd HH:mm:ss}  {TrimToWidth(log.Username, 16),-16}  {TrimToWidth(log.Action, 20),-20}  {(log.Success ? "OK" : "FAIL"),-7}  {TrimToWidth(log.Resource, 18)}");

            if (!string.IsNullOrWhiteSpace(log.Details))
            {
                foreach (var segment in WrapText($"Details: {log.Details}", 86))
                {
                    lines.Add($"  {segment}");
                }
            }

            var context = $"IP: {log.IpAddress} | Agent: {log.UserAgent}";
            foreach (var segment in WrapText(context, 86))
            {
                lines.Add($"  {segment}");
            }

            lines.Add(string.Empty);
        }

        if (logs.Count() == 0)
        {
            lines.Add("No audit records matched the selected filters.");
        }

        return lines;
    }

    private static string BuildFilterSummary(AuditLogQuery query)
    {
        var segments = new List<string>
        {
            $"username={query.Username ?? "*"}",
            $"action={query.ActionType ?? "*"}",
            $"resource={query.Resource ?? "*"}",
            $"from={query.FromUtc?.ToString("O") ?? "*"}",
            $"to={query.ToUtc?.ToString("O") ?? "*"}",
            $"success={(query.Success.HasValue ? (query.Success.Value ? "true" : "false") : "*")}"
        };

        return string.Join("; ", segments);
    }

    private static List<List<string>> Paginate(IReadOnlyList<string> lines, int linesPerPage)
    {
        var pages = new List<List<string>>();

        for (var index = 0; index < lines.Count; index += linesPerPage)
        {
            pages.Add(lines.Skip(index).Take(linesPerPage).ToList());
        }

        if (pages.Count == 0)
        {
            pages.Add(new List<string> { "Compliance Audit Export", string.Empty, "No data." });
        }

        return pages;
    }

    private static byte[] BuildPdfDocument(IReadOnlyList<List<string>> pages)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            BuildPagesObject(pages.Count),
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"
        };

        for (var index = 0; index < pages.Count; index++)
        {
            var pageObjectNumber = 4 + (index * 2);
            var contentObjectNumber = pageObjectNumber + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectNumber} 0 R >>");

            var content = BuildContentStream(pages[index]);
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");

        var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(builder.Length);
            builder.AppendFormat("{0} 0 obj\n{1}\nendobj\n", index + 1, objects[index]);
        }

        var xrefOffset = builder.Length;
        builder.AppendFormat("xref\n0 {0}\n", objects.Count + 1);
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.AppendFormat("{0:0000000000} 00000 n \n", offset);
        }

        builder.Append("trailer\n");
        builder.AppendFormat("<< /Size {0} /Root 1 0 R >>\n", objects.Count + 1);
        builder.AppendFormat("startxref\n{0}\n%%EOF", xrefOffset);

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string BuildPagesObject(int pageCount)
    {
        var kids = Enumerable.Range(0, pageCount)
            .Select(index => $"{4 + (index * 2)} 0 R");

        return $"<< /Type /Pages /Kids [ {string.Join(' ', kids)} ] /Count {pageCount} >>";
    }

    private static string BuildContentStream(IEnumerable<string> lines)
    {
        var stream = new StringBuilder();
        stream.Append("BT\n/F1 9 Tf\n14 TL\n40 760 Td\n");

        foreach (var line in lines)
        {
            stream.Append('(');
            stream.Append(EscapePdfString(line));
            stream.Append(") Tj\nT*\n");
        }

        stream.Append("ET");
        return stream.ToString();
    }

    private static string EscapePdfString(string input)
    {
        var normalized = input
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

        var sanitized = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            sanitized.Append(ch is >= ' ' and <= '~' ? ch : '?');
        }

        return sanitized.ToString();
    }

    private static IEnumerable<string> WrapText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var remaining = text.Trim();
        while (remaining.Length > maxLength)
        {
            var splitIndex = remaining.LastIndexOf(' ', maxLength);
            if (splitIndex <= 0)
            {
                splitIndex = maxLength;
            }

            yield return remaining[..splitIndex].TrimEnd();
            remaining = remaining[splitIndex..].TrimStart();
        }

        if (remaining.Length > 0)
        {
            yield return remaining;
        }
    }

    private static string Quote(string? value)
    {
        var safeValue = value ?? string.Empty;
        return $"\"{safeValue.Replace("\"", "\"\"", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal)}\"";
    }

    private static string TrimToWidth(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }
}