using ReportTree.Server.Models;

namespace ReportTree.Server.Services;

public class DemoContentService
{
    public IReadOnlyList<Page> GetDemoPages()
    {
        var pages = new List<Page>();

        // Root demo overview
        pages.Add(new Page
        {
            Id = 9001,
            Title = "Demo Overview",
            Icon = "Dashboard20",
            ParentId = null,
            Order = 0,
            IsPublic = true,
            IsDemo = true,
            AllowedGroups = new List<string>(),
            AllowedUsers = new List<string>(),
            Layout = GetOverviewLayout()
        });

        // Demo content detail
        pages.Add(new Page
        {
            Id = 9002,
            Title = "Sample Insights",
            Icon = "ChartBar20",
            ParentId = 9001,
            Order = 1,
            IsPublic = true,
            IsDemo = true,
            AllowedGroups = new List<string>(),
            AllowedUsers = new List<string>(),
            Layout = GetInsightsLayout()
        });

        return pages;
    }

    private string GetOverviewLayout()
    {
        var layout = new[]
        {
            new
            {
                i = "panel-0",
                x = 0,
                y = 0,
                w = 6,
                h = 6,
                minW = 3,
                minH = 3,
                componentType = "simple-html",
                componentConfig = new
                {
                    content = "<h2>Welcome to Demo Mode</h2><p>This workspace is populated with safe, sample content so you can explore layouts without connecting to a tenant.</p><ul><li>Drag cards around to try responsive layouts.</li><li>Open the Tools Panel to add Power BI tiles.</li><li>Use the Admin area to see default roles.</li></ul>"
                },
                metadata = new
                {
                    title = "Demo Introduction",
                    description = "Explains how demo mode works",
                    createdAt = DateTime.UtcNow.ToString("o"),
                    updatedAt = DateTime.UtcNow.ToString("o")
                }
            },
            new
            {
                i = "panel-1",
                x = 6,
                y = 0,
                w = 6,
                h = 6,
                minW = 3,
                minH = 3,
                componentType = "simple-html",
                componentConfig = new
                {
                    content = "<h3>Quick-start assets</h3><p>Download the sample dataset and review the sample report preview to understand expected schema.</p><ul><li><a href=\"/sample-data/sample-sales.csv\" target=\"_blank\">Sample dataset (CSV)</a></li><li><a href=\"/onboarding/sample-report.svg\" target=\"_blank\">Sample report preview</a></li></ul><p>Follow the README quick-start to swap these out for real tenant data.</p>"
                },
                metadata = new
                {
                    title = "Sample Assets",
                    description = "Links to starter dataset and report preview",
                    createdAt = DateTime.UtcNow.ToString("o"),
                    updatedAt = DateTime.UtcNow.ToString("o")
                }
            }
        };

        return System.Text.Json.JsonSerializer.Serialize(layout);
    }

    private string GetInsightsLayout()
    {
        var layout = new[]
        {
            new
            {
                i = "panel-0",
                x = 0,
                y = 0,
                w = 12,
                h = 8,
                minW = 4,
                minH = 4,
                componentType = "simple-html",
                componentConfig = new
                {
                    content = "<h3>Sample Sales Performance</h3><p>This tile mirrors the KPIs from the bundled dataset. Replace the image with your own Power BI embed when you're ready.</p><img src=\"/onboarding/sample-report.svg\" alt=\"Sample report preview\" style=\"width:100%; height:auto; border-radius:6px; margin-top:12px;\"/>"
                },
                metadata = new
                {
                    title = "Sample Sales Report",
                    description = "Static preview of demo report",
                    createdAt = DateTime.UtcNow.ToString("o"),
                    updatedAt = DateTime.UtcNow.ToString("o")
                }
            }
        };

        return System.Text.Json.JsonSerializer.Serialize(layout);
    }
}
