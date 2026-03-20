namespace tx_repo_demo.Models;

public class DocumentListViewModel
{
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int CurrentVersion { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<DocumentVersionViewModel> Versions { get; set; } = new();
}
