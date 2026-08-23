namespace DriveMediaDedup.Models;

public class MediaFile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool IsImage => MimeType.StartsWith("image/");
    public bool IsVideo => MimeType.StartsWith("video/");
}