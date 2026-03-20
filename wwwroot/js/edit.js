(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const config = window.editConfig || {};

        window.saveDocument = saveDocument;
        window.confirmSave = confirmSave;

        function saveDocument() {
            var saveModal = new bootstrap.Modal(document.getElementById('saveModal'));
            saveModal.show();
        }

        function confirmSave() {
            var comment = document.getElementById('saveComment').value;
            var saveButton = document.getElementById('confirmSaveButton');
            var originalButtonText = saveButton.innerHTML;

            saveButton.disabled = true;
            saveButton.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Saving...';

            TXTextControl.saveDocument(TXTextControl.StreamType.InternalUnicodeFormat, function (documentBytes) {
                if (documentBytes) {
                    var data = {
                        documentId: config.documentId,
                        content: documentBytes.data,
                        comment: comment || 'Document updated via web editor',
                        title: config.title,
                        author: config.author
                    };

                    fetch(config.saveUrl || '/Edit/SaveDocument', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        body: JSON.stringify(data)
                    })
                        .then(response => response.json())
                        .then(result => {
                            var saveModal = bootstrap.Modal.getInstance(document.getElementById('saveModal'));
                            saveModal.hide();

                            saveButton.disabled = false;
                            saveButton.innerHTML = originalButtonText;

                            if (result.success) {
                                showAlert(result.message, 'success');
                                document.getElementById('saveComment').value = '';

                                if (!result.isContentUnchanged) {
                                    setTimeout(function () {
                                        window.location.href = config.editUrl || '/Edit/Index?id=' + config.documentId;
                                    }, 2000);
                                }
                            } else {
                                showAlert(result.message, 'danger');
                            }
                        })
                        .catch(error => {
                            var saveModal = bootstrap.Modal.getInstance(document.getElementById('saveModal'));
                            saveModal.hide();

                            saveButton.disabled = false;
                            saveButton.innerHTML = originalButtonText;

                            showAlert('Error saving document: ' + error.message, 'danger');
                            console.error('Error:', error);
                        });
                } else {
                    saveButton.disabled = false;
                    saveButton.innerHTML = originalButtonText;
                    showAlert('Failed to retrieve document content.', 'danger');
                }
            });
        }

        function showAlert(message, type) {
            var alertDiv = document.createElement('div');
            alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
            alertDiv.innerHTML = `
                <i class="bi bi-${type === 'success' ? 'check-circle' : 'exclamation-triangle'}"></i> ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;

            var container = document.querySelector('.container');
            container.insertBefore(alertDiv, container.firstChild);

            setTimeout(function () {
                var alert = bootstrap.Alert.getInstance(alertDiv);
                if (alert) {
                    alert.close();
                } else {
                    alertDiv.remove();
                }
            }, 5000);
        }
    });
})();
