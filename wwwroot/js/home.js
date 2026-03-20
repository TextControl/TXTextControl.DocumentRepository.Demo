(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const config = window.homeConfig || {};
        let deleteModalInstance = null;

        // Row expand/collapse
        document.querySelectorAll('.document-row').forEach(function (row) {
            row.addEventListener('click', function (e) {
                if (e.target.closest('.status-cell') || e.target.closest('.row-action-cell')) {
                    return;
                }

                var targetSelector = this.getAttribute('data-collapse-target');
                var collapseElement = document.querySelector(targetSelector);
                var collapse = new bootstrap.Collapse(collapseElement, { toggle: true });

                var icon = this.querySelector('.expand-icon');
                if (icon.classList.contains('bi-chevron-right')) {
                    icon.classList.replace('bi-chevron-right', 'bi-chevron-down');
                } else {
                    icon.classList.replace('bi-chevron-down', 'bi-chevron-right');
                }
            });
        });

        // Status dropdown clicks
        document.querySelectorAll('.dropdown-item[data-status]').forEach(function (item) {
            item.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                updateStatus(this.getAttribute('data-document-id'), this.getAttribute('data-status'));
            });
        });

        // Search form cleanup
        var searchForm = document.getElementById('searchForm');
        if (searchForm) {
            searchForm.addEventListener('submit', function () {
                var inputs = this.querySelectorAll('input[type="text"], select');
                inputs.forEach(function (input) {
                    if (!input.value.trim()) {
                        input.removeAttribute('name');
                    }
                });
            });
        }

        // Enter to submit search
        var searchQueryInput = document.getElementById('searchQuery');
        if (searchQueryInput) {
            searchQueryInput.addEventListener('keypress', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    searchForm && searchForm.submit();
                }
            });
        }

        // Highlight search terms
        var searchQuery = (window.homeConfig && window.homeConfig.searchQuery) || '';
        if (searchQuery) {
            highlightSearchTerms(searchQuery);
        }

        // Create document form
        var createForm = document.getElementById('createDocumentForm');
        if (createForm) {
            createForm.addEventListener('submit', function (e) {
                var titleInput = document.getElementById('documentTitle');
                if (!titleInput.value.trim()) {
                    e.preventDefault();
                    titleInput.classList.add('is-invalid');
                    return false;
                }
                var button = document.getElementById('createDocumentButton');
                var originalText = button.innerHTML;
                button.disabled = true;
                button.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Creating...';
            });
        }

        var createModalEl = document.getElementById('createDocumentModal');
        if (createModalEl) {
            createModalEl.addEventListener('hidden.bs.modal', function () {
                createForm && createForm.reset();
                var titleInput = document.getElementById('documentTitle');
                titleInput && titleInput.classList.remove('is-invalid');
                var button = document.getElementById('createDocumentButton');
                if (button) {
                    button.disabled = false;
                    button.innerHTML = '<i class="bi bi-file-earmark-plus"></i> Create & Edit';
                }
            });
        }

        var titleInputEl = document.getElementById('documentTitle');
        if (titleInputEl) {
            titleInputEl.addEventListener('input', function () {
                this.classList.remove('is-invalid');
            });
        }

        // Expose functions used by inline handlers
        window.openDeleteModal = openDeleteModal;
        window.confirmDeleteDocument = confirmDeleteDocument;
        window.restoreVersion = restoreVersion;
        window.confirmRestore = confirmRestore;

        // Auto-dismiss alerts after 5 seconds
        setTimeout(function () {
            var alerts = document.querySelectorAll('.alert');
            alerts.forEach(function (alert) {
                var bsAlert = new bootstrap.Alert(alert);
                bsAlert.close();
            });
        }, 5000);

        // Functions
        function highlightSearchTerms(query) {
            var terms = query.toLowerCase().split(' ');
            var tableCells = document.querySelectorAll('.table tbody td:nth-child(2), .table tbody td:nth-child(3)');
            tableCells.forEach(function (cell) {
                var text = cell.textContent;
                terms.forEach(function (term) {
                    if (term.length > 2) {
                        var regex = new RegExp('(' + term + ')', 'gi');
                        var highlightedText = text.replace(regex, '<mark>$1</mark>');
                        if (highlightedText !== text) {
                            cell.innerHTML = highlightedText;
                        }
                    }
                });
            });
        }

        function restoreVersion(documentId, versionNumber, currentVersion) {
            document.getElementById('restoreDocumentId').value = documentId;
            document.getElementById('restoreVersionNumber').value = versionNumber;
            document.getElementById('restoreCurrentVersion').value = currentVersion;
            document.getElementById('restoreVersionDisplay').textContent = versionNumber;
            document.getElementById('restoreVersionDisplay2').textContent = versionNumber;
            var restoreModal = new bootstrap.Modal(document.getElementById('restoreModal'));
            restoreModal.show();
        }

        function confirmRestore() {
            document.getElementById('restoreForm').submit();
        }

        function updateStatus(documentId, newStatus) {
            var dropdown = document.getElementById('statusDropdown-' + documentId);
            var originalText = dropdown.innerHTML;
            dropdown.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Updating...';
            dropdown.disabled = true;

            fetch(config.updateStatusUrl || '/Home/UpdateStatus', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ documentId: documentId, status: newStatus })
            })
                .then(response => response.json())
                .then(result => {
                    if (result.success) {
                        dropdown.classList.remove('btn-success', 'btn-warning', 'btn-info');
                        if (newStatus === 'active') dropdown.classList.add('btn-success');
                        else if (newStatus === 'archived') dropdown.classList.add('btn-warning');
                        else if (newStatus === 'draft') dropdown.classList.add('btn-info');
                        dropdown.innerHTML = '<i class="bi bi-circle-fill"></i> ' + newStatus;
                        dropdown.disabled = false;
                        showAlert(result.message, 'success');
                    } else {
                        dropdown.innerHTML = originalText;
                        dropdown.disabled = false;
                        showAlert(result.message, 'danger');
                    }
                })
                .catch(error => {
                    dropdown.innerHTML = originalText;
                    dropdown.disabled = false;
                    showAlert('Error updating status: ' + error.message, 'danger');
                    console.error('Error:', error);
                });
        }

        function openDeleteModal(documentId, title) {
            var idInput = document.getElementById('deleteDocumentId');
            var titleEl = document.getElementById('deleteDocumentTitle');
            var modalEl = document.getElementById('deleteModal');

            if (!idInput || !titleEl || !modalEl) {
                showAlert('Delete dialog could not be opened.', 'danger');
                return;
            }

            deleteModalInstance = bootstrap.Modal.getOrCreateInstance(modalEl);
            idInput.value = documentId;
            titleEl.textContent = title || 'this document';
            deleteModalInstance.show();
        }

        function confirmDeleteDocument() {
            var documentId = document.getElementById('deleteDocumentId').value;
            var button = document.getElementById('confirmDeleteButton');
            var originalText = button.innerHTML;
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Deleting...';

            fetch(config.deleteUrl || '/Home/Delete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(documentId)
            })
                .then(response => response.json())
                .then(result => {
                    button.disabled = false;
                    button.innerHTML = originalText;

                    if (deleteModalInstance) {
                        deleteModalInstance.hide();
                    }

                    if (result.success) {
                        showAlert(result.message, 'success');
                        setTimeout(function () { window.location.reload(); }, 800);
                    } else {
                        showAlert(result.message, 'danger');
                    }
                })
                .catch(error => {
                    button.disabled = false;
                    button.innerHTML = originalText;
                    showAlert('Error deleting document: ' + error.message, 'danger');
                    console.error('Error:', error);
                });
        }

        function showAlert(message, type) {
            var alertDiv = document.createElement('div');
            alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
            alertDiv.style.position = 'fixed';
            alertDiv.style.top = '20px';
            alertDiv.style.right = '20px';
            alertDiv.style.zIndex = '9999';
            alertDiv.style.minWidth = '300px';
            alertDiv.innerHTML = `
                <i class="bi bi-${type === 'success' ? 'check-circle' : 'exclamation-triangle'}"></i> ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;

            document.body.appendChild(alertDiv);

            setTimeout(function () {
                var alert = bootstrap.Alert.getInstance(alertDiv);
                if (alert) {
                    alert.close();
                } else {
                    alertDiv.remove();
                }
            }, 3000);
        }
    });
})();
