namespace tx_repo_demo.Models;

public class EditDocumentViewModel
{
    public Guid DocumentId { get; set; }
    public int Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public long FileSize { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsCurrentVersion { get; set; }
}
