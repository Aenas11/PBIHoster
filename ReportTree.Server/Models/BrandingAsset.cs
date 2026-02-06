namespace ReportTree.Server.Models;

public record BrandingAssetInfo(
    string Id,
    string FileName,
    string ContentType,
    long Length,
    DateTime UploadedAt
);

public sealed class BrandingAsset
{
    public BrandingAssetInfo Info { get; }
    public Stream Content { get; }

    public BrandingAsset(BrandingAssetInfo info, Stream content)
    {
        Info = info;
        Content = content;
    }
}
