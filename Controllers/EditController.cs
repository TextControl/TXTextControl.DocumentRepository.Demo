using Microsoft.AspNetCore.Mvc;
using tx_repo_demo.Models;
using TXTextControl.DocumentRepository.Repositories;
using TXTextControl.DocumentRepository.Extensions;

namespace tx_repo_demo.Controllers
{
    public class EditController : Controller
    {
        private readonly IFileDocumentRepository _documentRepository;

        public EditController(IFileDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        public async Task<IActionResult> Index(Guid id, int? version)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Document ID is required.");
            }

            try
            {
                // Get document info to retrieve metadata
                var documentInfo = await _documentRepository.GetDocumentAsync(id);
                
                // Determine which version to load
                var versionToLoad = version ?? documentInfo.Metadata.CurrentVersion;
                
                // Get the version metadata
                var versionMetadata = documentInfo.Versions.FirstOrDefault(v => v.Version == versionToLoad);
                if (versionMetadata == null)
                {
                    return NotFound($"Version {versionToLoad} not found for document {id}");
                }

                // Load the version content (byte array)
                var content = await _documentRepository.GetVersionContentAsync(id, versionToLoad);

                // Create view model
                var viewModel = new EditDocumentViewModel
                {
                    DocumentId = id,
                    Version = versionToLoad,
                    Title = documentInfo.Metadata.Title,
                    FileName = versionMetadata.FileName,
                    MimeType = versionMetadata.MimeType,
                    Content = content,
                    FileSize = versionMetadata.FileSize,
                    CreatedBy = versionMetadata.CreatedBy,
                    CreatedAtUtc = versionMetadata.CreatedAtUtc,
                    Comment = versionMetadata.Comment,
                    IsCurrentVersion = versionToLoad == documentInfo.Metadata.CurrentVersion
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return NotFound($"Error loading document: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocument(Guid id, int version)
        {
            try
            {
                var content = await _documentRepository.GetVersionContentAsync(id, version);
                return File(content, "application/octet-stream");
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveDocument([FromBody] SaveDocumentModel model)
        {
            if (model.DocumentId == Guid.Empty || model.Content == null || model.Content.Length == 0)
            {
                return BadRequest(new { success = false, message = "Invalid document data." });
            }

            try
            {
                // Get current document info
                var documentInfo = await _documentRepository.GetDocumentAsync(model.DocumentId);

                // Create a temporary ServerTextControl instance to load the document
                using var textControl = new TXTextControl.ServerTextControl();
                textControl.Create();

                // Load the byte array into the ServerTextControl
                textControl.Load(model.Content, TXTextControl.BinaryStreamType.InternalUnicodeFormat);

                // Set DocumentSettings - these will be used by SaveToRepositoryAsync
                textControl.DocumentSettings.DocumentTitle = model.Title ?? documentInfo.Metadata.Title;
                textControl.DocumentSettings.DocumentSubject = documentInfo.Metadata.Subject;
                textControl.DocumentSettings.Author = User.Identity?.Name ?? model.Author ?? "Anonymous";
                
                if (!string.IsNullOrEmpty(model.Tags))
                {
                    textControl.DocumentSettings.DocumentKeywords = model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }

                // Use the SaveToRepositoryAsync extension method to save a new version
                var versionMetadata = await textControl.SaveToRepositoryAsync(
                    repository: _documentRepository,
                    documentId: model.DocumentId,
                    comment: model.Comment ?? "Document updated via web editor",
                    fileName: documentInfo.Versions.LastOrDefault()?.FileName ?? "document.tx",
                    streamType: TXTextControl.BinaryStreamType.InternalUnicodeFormat,
                    cancellationToken: HttpContext.RequestAborted
                );

                return Ok(new 
                { 
                    success = true, 
                    message = versionMetadata.IsContentUnchanged 
                        ? "No changes detected. Document not saved." 
                        : $"Document saved successfully as version {versionMetadata.Version}.",
                    version = versionMetadata.Version,
                    isContentUnchanged = versionMetadata.IsContentUnchanged
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error saving document: {ex.Message}" });
            }
        }
    }

    public class SaveDocumentModel
    {
        public Guid DocumentId { get; set; }
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string? Title { get; set; }
        public string? Comment { get; set; }
        public string? Tags { get; set; }
        public string? Author { get; set; }
    }
}
