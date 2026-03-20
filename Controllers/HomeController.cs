using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using tx_repo_demo.Models;
using TXTextControl.DocumentRepository.Extensions;
using TXTextControl.DocumentRepository.Repositories;
using TXTextControl.DocumentRepository.Models;

namespace tx_repo_demo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFileDocumentRepository _documentRepository;

        public HomeController(IFileDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        public async Task<IActionResult> Index(string? q, string? tags, string? author, string? status)
        {
            var searchViewModel = new DocumentSearchViewModel
            {
                SearchQuery = q,
                SearchTags = tags,
                SearchAuthor = author,
                SearchStatus = status
            };

            // Use search if any search parameters are provided
            if (searchViewModel.IsSearchActive)
            {
                var searchRequest = new SearchDocumentsRequest
                {
                    TitleContains = q,
                    SubjectContains = q, // Search in both title and subject
                    RequireAnyTag = !string.IsNullOrWhiteSpace(tags) 
                        ? tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() 
                        : null,
                    CreatedByContains = author,
                    Status = status,
                    SortBy = "UpdatedAtUtc",
                    SortAscending = false,
                    Skip = 0,
                    MaxResults = 100
                };

                var searchResult = await _documentRepository.SearchDocumentsAsync(searchRequest);
                
                searchViewModel.TotalResults = searchResult.TotalCount;
                searchViewModel.DisplayedResults = searchResult.ReturnedCount;
                searchViewModel.HasMore = searchResult.HasMore;

                // Get detailed document info with versions for search results
                foreach (var doc in searchResult.Documents)
                {
                    var documentInfo = await _documentRepository.GetDocumentAsync(doc.DocumentId);
                    
                    searchViewModel.Documents.Add(new DocumentListViewModel
                    {
                        DocumentId = doc.DocumentId,
                        Title = doc.Title,
                        Subject = doc.Subject,
                        CreatedBy = doc.CreatedBy,
                        CreatedAtUtc = doc.CreatedAtUtc,
                        UpdatedAtUtc = doc.UpdatedAtUtc,
                        CurrentVersion = doc.CurrentVersion,
                        Status = doc.Status,
                        Tags = doc.Tags.ToList(),
                        Versions = documentInfo.Versions.Select(v => new DocumentVersionViewModel
                        {
                            Version = v.Version,
                            FileName = v.FileName,
                            MimeType = v.MimeType,
                            FileSize = v.FileSize,
                            CreatedBy = v.CreatedBy,
                            CreatedAtUtc = v.CreatedAtUtc,
                            Comment = v.Comment,
                            RestoredFromVersion = v.RestoredFromVersion
                        }).ToList()
                    });
                }
            }
            else
            {
                // No search - list all documents
                var documents = await _documentRepository.ListDocumentsAsync();
                
                foreach (var doc in documents)
                {
                    var documentInfo = await _documentRepository.GetDocumentAsync(doc.DocumentId);
                    
                    searchViewModel.Documents.Add(new DocumentListViewModel
                    {
                        DocumentId = doc.DocumentId,
                        Title = doc.Title,
                        Subject = doc.Subject,
                        CreatedBy = doc.CreatedBy,
                        CreatedAtUtc = doc.CreatedAtUtc,
                        UpdatedAtUtc = doc.UpdatedAtUtc,
                        CurrentVersion = doc.CurrentVersion,
                        Status = doc.Status,
                        Tags = doc.Tags.ToList(),
                        Versions = documentInfo.Versions.Select(v => new DocumentVersionViewModel
                        {
                            Version = v.Version,
                            FileName = v.FileName,
                            MimeType = v.MimeType,
                            FileSize = v.FileSize,
                            CreatedBy = v.CreatedBy,
                            CreatedAtUtc = v.CreatedAtUtc,
                            Comment = v.Comment,
                            RestoredFromVersion = v.RestoredFromVersion
                        }).ToList()
                    });
                }
                
                searchViewModel.TotalResults = searchViewModel.Documents.Count;
                searchViewModel.DisplayedResults = searchViewModel.Documents.Count;
            }

            return View(searchViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> RestoreVersion(Guid documentId, int versionToRestore, int currentVersion)
        {
            try
            {
                await _documentRepository.RestoreVersionAsync(
                    documentId, 
                    versionToRestore, 
                    currentVersion, 
                    User.Identity?.Name ?? "Anonymous",
                    $"Restored from version {versionToRestore}");

                TempData["SuccessMessage"] = $"Version {versionToRestore} has been successfully restored.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to restore version: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewDocument(string title, string subject, string tags)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] = "Document title is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var author = User.Identity?.Name ?? "Anonymous";
                
                // Create a temporary ServerTextControl to generate blank document content
                using var textControl = new TXTextControl.ServerTextControl();
                textControl.Create();

                // Set document settings
                textControl.DocumentSettings.DocumentTitle = title;
                textControl.DocumentSettings.DocumentSubject = subject ?? string.Empty;
                textControl.DocumentSettings.Author = author;
                textControl.DocumentSettings.CreationDate = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(tags))
                {
                    textControl.DocumentSettings.DocumentKeywords = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }

                // Use the SaveToRepositoryAsNewAsync extension method to create the document
                var versionMetadata = await textControl.SaveToRepositoryAsNewAsync(
                    repository: _documentRepository,
                    fileName: $"{SanitizeFileName(title)}.tx",
                    streamType: TXTextControl.BinaryStreamType.InternalUnicodeFormat,
                    comment: "Initial document creation",
                    cancellationToken: HttpContext.RequestAborted
                );

                TempData["SuccessMessage"] = $"Document '{title}' created successfully.";
                
                // Redirect to the Edit controller to open the new document
                return RedirectToAction("Index", "Edit", new { id = versionMetadata.DocumentId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to create document: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid id, int version)
        {
            try
            {
                var documentInfo = await _documentRepository.GetDocumentAsync(id);
                var versionMetadata = documentInfo.Versions.FirstOrDefault(v => v.Version == version);

                if (versionMetadata == null)
                {
                    return NotFound();
                }

                var content = await _documentRepository.GetVersionContentAsync(id, version);
                var mimeType = string.IsNullOrWhiteSpace(versionMetadata.MimeType) ? "application/octet-stream" : versionMetadata.MimeType;
                var fileName = string.IsNullOrWhiteSpace(versionMetadata.FileName) ? $"document_{version}.bin" : versionMetadata.FileName;

                return File(content, mimeType, fileName);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusModel model)
        {
            if (model.DocumentId == Guid.Empty || string.IsNullOrWhiteSpace(model.Status))
            {
                return BadRequest(new { success = false, message = "Invalid request data." });
            }

            try
            {
                // Use the repository method to update status
                await _documentRepository.UpdateDocumentStatusAsync(
                    model.DocumentId, 
                    model.Status, 
                    HttpContext.RequestAborted);

                return Ok(new 
                { 
                    success = true, 
                    message = $"Document status updated to '{model.Status}'.",
                    status = model.Status
                });
            }
            catch (TXTextControl.DocumentRepository.Exceptions.DocumentNotFoundException)
            {
                return NotFound(new { success = false, message = "Document not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error updating status: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] Guid documentId)
        {
            if (documentId == Guid.Empty)
            {
                return BadRequest(new { success = false, message = "Invalid document id." });
            }

            try
            {
                await _documentRepository.DeleteDocumentAsync(documentId, HttpContext.RequestAborted);
                return Ok(new { success = true, message = "Document deleted." });
            }
            catch (TXTextControl.DocumentRepository.Exceptions.DocumentNotFoundException)
            {
                return NotFound(new { success = false, message = "Document not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error deleting document: {ex.Message}" });
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "document";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "document" : sanitized;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class UpdateStatusModel
    {
        public Guid DocumentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
