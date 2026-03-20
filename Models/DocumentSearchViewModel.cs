namespace tx_repo_demo.Models;

public class DocumentSearchViewModel
{
    public List<DocumentListViewModel> Documents { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string? SearchTags { get; set; }
    public string? SearchAuthor { get; set; }
    public string? SearchStatus { get; set; }
    public int TotalResults { get; set; }
    public int DisplayedResults { get; set; }
    public bool HasMore { get; set; }
    public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchQuery) || 
                                  !string.IsNullOrWhiteSpace(SearchTags) || 
                                  !string.IsNullOrWhiteSpace(SearchAuthor) ||
                                  !string.IsNullOrWhiteSpace(SearchStatus);
}
