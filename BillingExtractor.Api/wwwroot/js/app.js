// Billing Extractor - Main Application JavaScript
class BillingExtractorApp {
    constructor() {
        this.state = {
            uploadedFiles: [],
            processingResults: null,
            processingHistory: JSON.parse(localStorage.getItem('billingExtractorHistory')) || [],
            apiStatus: 'checking',
            selectedInvoice: null
        };

        this.init();
    }

    init() {
        this.cacheElements();
        this.bindEvents();
        this.checkApiStatus();
        this.loadHistory();
        this.updateStorageUsage();
        this.setupDragAndDrop();
    }

    cacheElements() {
        // File upload elements
        this.fileInput = document.getElementById('fileInput');
        this.uploadArea = document.getElementById('uploadArea');
        this.fileList = document.getElementById('fileList');
        this.processBtn = document.getElementById('processBtn');
        this.clearBtn = document.getElementById('clearBtn');

        // Processing options
        this.enableValidation = document.getElementById('enableValidation');
        this.enableDuplicateDetection = document.getElementById('enableDuplicateDetection');

        // Loading overlay
        this.loadingOverlay = document.getElementById('loadingOverlay');
        this.processingFiles = document.getElementById('processingFiles');
        this.processingStatus = document.getElementById('processingStatus');
        this.progressBar = document.getElementById('progressBar');

        // Results elements
        this.resultsSection = document.getElementById('resultsSection');
        this.resultsStats = document.getElementById('resultsStats');
        this.processedCount = document.getElementById('processedCount');
        this.warningCount = document.getElementById('warningCount');
        this.errorCount = document.getElementById('errorCount');

        // Tabs
        this.resultsTabs = document.getElementById('resultsTabs');
        this.tabContents = {
            extracted: document.getElementById('extractedTab'),
            history: document.getElementById('historyTab'),
            validation: document.getElementById('validationTab')
        };

        // Containers
        this.invoicesContainer = document.getElementById('invoicesContainer');
        this.historyContainer = document.getElementById('historyContainer');
        this.validationContainer = document.getElementById('validationContainer');

        // API status
        this.statusDot = document.getElementById('statusDot');
        this.apiStatus = document.getElementById('apiStatus');

        // Footer elements
        this.storageUsage = document.getElementById('storageUsage');
        this.lastUpdated = document.getElementById('lastUpdated');

        // Modal
        this.invoiceModal = document.getElementById('invoiceModal');
        this.modalClose = document.getElementById('modalClose');
        this.modalTitle = document.getElementById('modalTitle');
        this.modalBody = document.getElementById('modalBody');
    }

    bindEvents() {
        // File upload events
        this.fileInput.addEventListener('change', (e) => this.handleFileSelect(e));
        this.uploadArea.addEventListener('click', () => this.fileInput.click());
        this.processBtn.addEventListener('click', () => this.processFiles());
        this.clearBtn.addEventListener('click', () => this.clearFiles());

        // Tab switching
        this.resultsTabs.addEventListener('click', (e) => {
            const tab = e.target.closest('.tab');
            if (tab && tab.dataset.tab) {
                this.switchTab(tab.dataset.tab);
            }
        });

        // Modal
        this.modalClose.addEventListener('click', () => this.closeModal());
        this.invoiceModal.addEventListener('click', (e) => {
            if (e.target === this.invoiceModal) {
                this.closeModal();
            }
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') this.closeModal();
            if (e.key === 'u' && e.ctrlKey) {
                e.preventDefault();
                this.fileInput.click();
            }
        });
    }

    setupDragAndDrop() {
        this.uploadArea.addEventListener('dragover', (e) => {
            e.preventDefault();
            this.uploadArea.classList.add('drag-over');
        });

        this.uploadArea.addEventListener('dragleave', () => {
            this.uploadArea.classList.remove('drag-over');
        });

        this.uploadArea.addEventListener('drop', (e) => {
            e.preventDefault();
            this.uploadArea.classList.remove('drag-over');
            this.handleFileDrop(e.dataTransfer.files);
        });
    }

    async checkApiStatus() {
        try {
            const response = await fetch('/health');
            if (response.ok) {
                this.setApiStatus('online', 'API Connected');
            } else {
                this.setApiStatus('error', 'API Error');
            }
        } catch (error) {
            this.setApiStatus('offline', 'API Offline');
            console.warn('API status check failed:', error);
        }
    }

    setApiStatus(status, message) {
        const statusMap = {
            online: { color: '#4ade80', text: 'Online' },
            error: { color: '#f72585', text: 'Error' },
            offline: { color: '#6c757d', text: 'Offline' },
            checking: { color: '#ffb703', text: 'Checking...' }
        };

        const statusInfo = statusMap[status] || statusMap.offline;
        this.statusDot.style.backgroundColor = statusInfo.color;
        this.apiStatus.textContent = message || statusInfo.text;
        this.state.apiStatus = status;
    }

    handleFileSelect(event) {
        const files = Array.from(event.target.files);
        this.addFiles(files);
    }

    handleFileDrop(files) {
        this.addFiles(Array.from(files));
    }

    addFiles(files) {
        const validFiles = files.filter(file => {
            const validTypes = ['application/pdf', 'image/jpeg', 'image/jpg', 'image/png'];
            return validTypes.includes(file.type) ||
                   file.name.match(/\.(pdf|jpg|jpeg|png)$/i);
        });

        if (validFiles.length === 0 && files.length > 0) {
            this.showNotification('Please select valid files (PDF, JPG, PNG)', 'error');
            return;
        }

        if (validFiles.length > 0) {
            this.state.uploadedFiles = [...this.state.uploadedFiles, ...validFiles];
            this.updateFileList();
            this.processBtn.disabled = false;
            this.showNotification(`Added ${validFiles.length} file(s)`, 'success');
        }
    }

    removeFile(index) {
        this.state.uploadedFiles.splice(index, 1);
        this.updateFileList();
        this.processBtn.disabled = this.state.uploadedFiles.length === 0;
    }

    updateFileList() {
        if (this.state.uploadedFiles.length === 0) {
            this.fileList.innerHTML = `
                <div class="empty-state">
                    <i class="fas fa-file"></i>
                    <p>No files selected</p>
                </div>
            `;
            return;
        }

        this.fileList.innerHTML = this.state.uploadedFiles.map((file, index) => `
            <div class="file-item">
                <div class="file-info">
                    <div class="file-icon">
                        <i class="fas ${this.getFileIcon(file.name)}"></i>
                    </div>
                    <div class="file-details">
                        <div class="file-name" title="${file.name}">
                            ${this.truncateText(file.name, 40)}
                        </div>
                        <div class="file-size">
                            ${this.formatFileSize(file.size)}
                        </div>
                    </div>
                </div>
                <div class="file-actions">
                    <button class="remove-btn" onclick="app.removeFile(${index})" title="Remove file">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
            </div>
        `).join('');
    }

    getFileIcon(fileName) {
        const extension = fileName.split('.').pop().toLowerCase();
        switch (extension) {
            case 'pdf': return 'fa-file-pdf';
            case 'jpg':
            case 'jpeg':
            case 'png': return 'fa-file-image';
            default: return 'fa-file';
        }
    }

    async processFiles() {
        if (this.state.uploadedFiles.length === 0) return;

        const enableValidation = this.enableValidation.checked;
        const enableDuplicateDetection = this.enableDuplicateDetection.checked;

        this.showLoading(true);
        this.updateProcessingStatus('Uploading files...', 10);

        const formData = new FormData();
        this.state.uploadedFiles.forEach(file => {
            formData.append('files', file);
        });

        try {
            this.updateProcessingStatus('Processing with AI...', 30);

            const response = await fetch(`/api/invoices/upload?validate=${enableValidation}&checkDuplicates=${enableDuplicateDetection}`, {
                method: 'POST',
                body: formData
            });

            this.updateProcessingStatus('Validating results...', 70);

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            this.updateProcessingStatus('Finalizing...', 90);

            // Add to history
            const historyEntry = {
                id: Date.now(),
                timestamp: new Date().toISOString(),
                files: this.state.uploadedFiles.map(f => f.name),
                result: result,
                stats: {
                    processed: result.totalProcessed || 0,
                    failed: result.totalFailed || 0,
                    duplicates: result.totalDuplicates || 0
                }
            };

            this.state.processingHistory.unshift(historyEntry);
            this.state.processingResults = result;

            // Save to localStorage
            localStorage.setItem('billingExtractorHistory',
                JSON.stringify(this.state.processingHistory.slice(0, 50))); // Keep last 50 entries

            // Update UI
            this.displayResults();
            this.loadHistory();
            this.updateStorageUsage();
            this.updateProcessingStatus('Complete!', 100);

            // Show success message
            setTimeout(() => {
                this.showLoading(false);
                this.showNotification(`Processed ${result.totalProcessed || 0} invoice(s)`, 'success');

                // Clear uploaded files
                this.state.uploadedFiles = [];
                this.updateFileList();
                this.processBtn.disabled = true;
            }, 500);

        } catch (error) {
            console.error('Error processing files:', error);
            this.showLoading(false);
            this.showNotification(`Error: ${error.message}`, 'error');
        }
    }

    showLoading(show) {
        if (show) {
            this.loadingOverlay.classList.add('active');
            document.body.style.overflow = 'hidden';
        } else {
            this.loadingOverlay.classList.remove('active');
            document.body.style.overflow = '';
        }
    }

    updateProcessingStatus(status, progress) {
        this.processingStatus.textContent = status;
        this.progressBar.style.width = `${progress}%`;
        this.processingFiles.textContent = this.state.uploadedFiles.length;
    }

    displayResults() {
        if (!this.state.processingResults || !this.state.processingResults.invoices) {
            this.invoicesContainer.innerHTML = `
                <div class="empty-state">
                    <i class="fas fa-search"></i>
                    <h3>No Results</h3>
                    <p>No invoices were processed</p>
                </div>
            `;
            return;
        }

        // Update stats
        const invoices = this.state.processingResults.invoices;
        const totalProcessed = invoices.length;
        const totalWarnings = invoices.reduce((sum, inv) => sum + (inv.validationWarnings?.length || 0), 0);
        const totalErrors = invoices.reduce((sum, inv) => sum + (inv.validationErrors?.length || 0), 0);

        this.processedCount.textContent = totalProcessed;
        this.warningCount.textContent = totalWarnings;
        this.errorCount.textContent = totalErrors;

        // Display invoices
        this.invoicesContainer.innerHTML = invoices.map(invoice => this.createInvoiceCard(invoice)).join('');

        // Display validation details
        this.displayValidationDetails(invoices);

        // Show results section
        this.resultsSection.style.display = 'block';
        this.switchTab('extracted');
    }

    createInvoiceCard(invoice) {
        const hasErrors = invoice.validationErrors?.length > 0;
        const hasWarnings = invoice.validationWarnings?.length > 0;
        const statusClass = hasErrors ? 'error' : hasWarnings ? 'warning' : 'success';
        const statusText = hasErrors ? 'Error' : hasWarnings ? 'Warning' : 'Success';

        const lineItemsHtml = invoice.lineItems?.map(item => `
            <tr>
                <td>${item.description || 'N/A'}</td>
                <td>${item.quantity || 'N/A'}</td>
                <td>${item.unit || 'N/A'}</td>
                <td class="amount">${this.formatCurrency(item.unitPrice, invoice.currency)}</td>
                <td class="amount">${this.formatCurrency(item.lineTotal, invoice.currency)}</td>
            </tr>
        `).join('') || '';

        return `
            <div class="invoice-card ${statusClass}">
                <div class="invoice-header">
                    <div class="invoice-title">
                        <div>
                            <div class="invoice-number">${invoice.invoiceNumber || 'Unknown Invoice'}</div>
                            <div class="invoice-vendor">${invoice.vendorName || 'Unknown Vendor'}</div>
                        </div>
                    </div>
                    <span class="invoice-status status-${statusClass}">${statusText}</span>
                </div>

                <div class="invoice-details">
                    <div class="detail-item">
                        <span class="detail-label">Invoice Date</span>
                        <span class="detail-value">${this.formatDate(invoice.invoiceDate)}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Due Date</span>
                        <span class="detail-value">${this.formatDate(invoice.dueDate)}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Total Amount</span>
                        <span class="detail-value amount">${this.formatCurrency(invoice.totalAmount, invoice.currency)}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Tax Amount</span>
                        <span class="detail-value">${this.formatCurrency(invoice.taxAmount, invoice.currency)}</span>
                    </div>
                </div>

                ${invoice.lineItems?.length > 0 ? `
                <div class="line-items-section">
                    <h4>Line Items</h4>
                    <table class="line-items-table">
                        <thead>
                            <tr>
                                <th>Description</th>
                                <th>Quantity</th>
                                <th>Unit</th>
                                <th>Unit Price</th>
                                <th>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${lineItemsHtml}
                        </tbody>
                    </table>
                </div>
                ` : ''}

                ${(invoice.validationWarnings?.length > 0 || invoice.validationErrors?.length > 0) ? `
                <div class="invoice-actions">
                    <button class="action-btn view" onclick="app.showInvoiceDetails('${invoice.id}')">
                        <i class="fas fa-eye"></i> View Details
                    </button>
                </div>
                ` : ''}
            </div>
        `;
    }

    displayValidationDetails(invoices) {
        const allWarnings = invoices.flatMap(inv =>
            (inv.validationWarnings || []).map(w => ({...w, invoiceNumber: inv.invoiceNumber, type: 'warning'}))
        );
        const allErrors = invoices.flatMap(inv =>
            (inv.validationErrors || []).map(e => ({...e, invoiceNumber: inv.invoiceNumber, type: 'error'}))
        );

        const allValidations = [...allWarnings, ...allErrors];

        if (allValidations.length === 0) {
            this.validationContainer.innerHTML = `
                <div class="empty-state">
                    <i class="fas fa-check-circle"></i>
                    <h3>All Validations Passed</h3>
                    <p>No validation warnings or errors found</p>
                </div>
            `;
            return;
        }

        this.validationContainer.innerHTML = allValidations.map(validation => `
            <div class="validation-item ${validation.type}">
                <div class="validation-icon">
                    <i class="fas ${validation.type === 'error' ? 'fa-exclamation-circle' : 'fa-exclamation-triangle'}"></i>
                </div>
                <div class="validation-content">
                    <div class="validation-code">
                        ${validation.code} - Invoice: ${validation.invoiceNumber}
                    </div>
                    <div class="validation-message">
                        ${validation.message}
                    </div>
                </div>
            </div>
        `).join('');
    }

    loadHistory() {
        if (this.state.processingHistory.length === 0) {
            this.historyContainer.innerHTML = `
                <div class="empty-state">
                    <i class="fas fa-history"></i>
                    <h3>No History Yet</h3>
                    <p>Processed invoices will appear here</p>
                </div>
            `;
            return;
        }

        this.historyContainer.innerHTML = this.state.processingHistory.slice(0, 10).map(entry => `
            <div class="history-item" onclick="app.viewHistoryEntry('${entry.id}')">
                <div class="history-info">
                    <div class="history-timestamp">
                        ${this.formatDateTime(entry.timestamp)}
                    </div>
                    <div class="history-files">
                        <i class="fas fa-file"></i>
                        ${entry.files.length} file(s) - ${entry.stats.processed} processed,
                        ${entry.stats.failed} failed, ${entry.stats.duplicates} duplicates
                    </div>
                </div>
                <div class="history-action">
                    View Results
                </div>
            </div>
        `).join('');
    }

    viewHistoryEntry(entryId) {
        const entry = this.state.processingHistory.find(e => e.id === parseInt(entryId));
        if (entry) {
            this.state.processingResults = entry.result;
            this.displayResults();
            this.switchTab('extracted');
            this.showNotification('Loaded historical results', 'success');
        }
    }

    showInvoiceDetails(invoiceId) {
        const invoice = this.state.processingResults?.invoices?.find(inv => inv.id === invoiceId);
        if (!invoice) return;

        this.state.selectedInvoice = invoice;
        this.modalTitle.textContent = `Invoice Details: ${invoice.invoiceNumber}`;

        const detailsHtml = `
            <div class="invoice-details-modal">
                <div class="detail-section">
                    <h4>Basic Information</h4>
                    <div class="detail-grid">
                        <div><strong>Invoice Number:</strong> ${invoice.invoiceNumber}</div>
                        <div><strong>Vendor:</strong> ${invoice.vendorName}</div>
                        <div><strong>Customer:</strong> ${invoice.customerName}</div>
                        <div><strong>Invoice Date:</strong> ${this.formatDate(invoice.invoiceDate)}</div>
                        <div><strong>Due Date:</strong> ${this.formatDate(invoice.dueDate)}</div>
                        <div><strong>Status:</strong> ${invoice.status}</div>
                    </div>
                </div>

                <div class="detail-section">
                    <h4>Financial Details</h4>
                    <div class="detail-grid">
                        <div><strong>Currency:</strong> ${invoice.currency}</div>
                        <div><strong>Total Amount:</strong> ${this.formatCurrency(invoice.totalAmount, invoice.currency)}</div>
                        <div><strong>Tax Amount:</strong> ${this.formatCurrency(invoice.taxAmount, invoice.currency)}</div>
                        <div><strong>Subtotal:</strong> ${this.formatCurrency(invoice.subtotal, invoice.currency)}</div>
                    </div>
                </div>

                ${invoice.lineItems?.length > 0 ? `
                <div class="detail-section">
                    <h4>Line Items (${invoice.lineItems.length})</h4>
                    <table class="modal-table">
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>Description</th>
                                <th>Quantity</th>
                                <th>Unit</th>
                                <th>Unit Price</th>
                                <th>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${invoice.lineItems.map((item, index) => `
                                <tr>
                                    <td>${index + 1}</td>
                                    <td>${item.description}</td>
                                    <td>${item.quantity}</td>
                                    <td>${item.unit}</td>
                                    <td>${this.formatCurrency(item.unitPrice, invoice.currency)}</td>
                                    <td>${this.formatCurrency(item.lineTotal, invoice.currency)}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
                ` : ''}

                ${invoice.validationWarnings?.length > 0 ? `
                <div class="detail-section">
                    <h4>Validation Warnings (${invoice.validationWarnings.length})</h4>
                    <div class="validation-list">
                        ${invoice.validationWarnings.map(warning => `
                            <div class="validation-item warning">
                                <i class="fas fa-exclamation-triangle"></i>
                                <div>
                                    <strong>${warning.code}:</strong> ${warning.message}
                                </div>
                            </div>
                        `).join('')}
                    </div>
                </div>
                ` : ''}

                ${invoice.validationErrors?.length > 0 ? `
                <div class="detail-section">
                    <h4>Validation Errors (${invoice.validationErrors.length})</h4>
                    <div class="validation-list">
                        ${invoice.validationErrors.map(error => `
                            <div class="validation-item error">
                                <i class="fas fa-exclamation-circle"></i>
                                <div>
                                    <strong>${error.code}:</strong> ${error.message}
                                </div>
                            </div>
                        `).join('')}
                    </div>
                </div>
                ` : ''}

                ${invoice.processingError ? `
                <div class="detail-section">
                    <h4>Processing Error</h4>
                    <div class="error-message">
                        <i class="fas fa-bug"></i>
                        <div>${invoice.processingError}</div>
                    </div>
                </div>
                ` : ''}
            </div>
        `;

        this.modalBody.innerHTML = detailsHtml;
        this.invoiceModal.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    closeModal() {
        this.invoiceModal.classList.remove('active');
        document.body.style.overflow = '';
    }

    switchTab(tabName) {
        // Update active tab
        document.querySelectorAll('.tab').forEach(tab => {
            tab.classList.toggle('active', tab.dataset.tab === tabName);
        });

        // Show active content
        Object.keys(this.tabContents).forEach(key => {
            this.tabContents[key].classList.toggle('active', key === tabName);
        });

        // Scroll to results if needed
        if (tabName !== 'extracted' && this.state.processingResults) {
            this.resultsSection.scrollIntoView({ behavior: 'smooth' });
        }
    }

    clearFiles() {
        if (this.state.uploadedFiles.length === 0) return;

        if (confirm('Clear all uploaded files?')) {
            this.state.uploadedFiles = [];
            this.updateFileList();
            this.processBtn.disabled = true;
            this.showNotification('Cleared all files', 'success');
        }
    }

    updateStorageUsage() {
        const historySize = JSON.stringify(this.state.processingHistory).length;
        const usageMB = (historySize / (1024 * 1024)).toFixed(2);
        this.storageUsage.textContent = `${usageMB} MB`;
        this.lastUpdated.textContent = 'Just now';
    }

    showNotification(message, type = 'info') {
        // Remove existing notifications
        const existingNotifications = document.querySelectorAll('.notification');
        existingNotifications.forEach(n => n.remove());

        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.innerHTML = `
            <i class="fas ${type === 'success' ? 'fa-check-circle' : type === 'error' ? 'fa-exclamation-circle' : 'fa-info-circle'}"></i>
            <span>${message}</span>
            <button class="notification-close" onclick="this.parentElement.remove()">
                <i class="fas fa-times"></i>
            </button>
        `;

        document.body.appendChild(notification);

        // Add CSS for notification
        if (!document.querySelector('#notification-styles')) {
            const style = document.createElement('style');
            style.id = 'notification-styles';
            style.textContent = `
                .notification {
                    position: fixed;
                    top: 20px;
                    right: 20px;
                    padding: 1rem 1.5rem;
                    border-radius: 8px;
                    background: white;
                    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                    display: flex;
                    align-items: center;
                    gap: 0.75rem;
                    z-index: 10000;
                    animation: slideInRight 0.3s ease;
                    max-width: 400px;
                }
                @keyframes slideInRight {
                    from { transform: translateX(100%); opacity: 0; }
                    to { transform: translateX(0); opacity: 1; }
                }
                .notification.success {
                    border-left: 4px solid #4cc9f0;
                }
                .notification.error {
                    border-left: 4px solid #f72585;
                }
                .notification.info {
                    border-left: 4px solid #4361ee;
                }
                .notification-close {
                    background: none;
                    border: none;
                    color: #6c757d;
                    cursor: pointer;
                    margin-left: auto;
                }
            `;
            document.head.appendChild(style);
        }

        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentElement) {
                notification.remove();
            }
        }, 5000);
    }

    // Utility methods
    truncateText(text, maxLength) {
        return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    formatDate(dateString) {
        if (!dateString) return 'N/A';
        try {
            const date = new Date(dateString);
            return date.toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric'
            });
        } catch {
            return 'Invalid Date';
        }
    }

    formatDateTime(dateTimeString) {
        if (!dateTimeString) return 'N/A';
        try {
            const date = new Date(dateTimeString);
            return date.toLocaleString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        } catch {
            return 'Invalid Date';
        }
    }

    formatCurrency(amount, currency = 'USD') {
        if (amount == null) return 'N/A';
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: currency || 'USD'
        }).format(amount);
    }
}

// Initialize the application when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.app = new BillingExtractorApp();

    // Add some sample CSS for modal tables
    const modalStyles = document.createElement('style');
    modalStyles.textContent = `
        .invoice-details-modal .detail-section {
            margin-bottom: 2rem;
            padding-bottom: 1.5rem;
            border-bottom: 1px solid #dee2e6;
        }
        .invoice-details-modal .detail-section:last-child {
            border-bottom: none;
        }
        .invoice-details-modal .detail-section h4 {
            margin-bottom: 1rem;
            color: #4361ee;
        }
        .invoice-details-modal .detail-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
            gap: 1rem;
        }
        .modal-table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 1rem;
        }
        .modal-table th,
        .modal-table td {
            padding: 0.75rem;
            border-bottom: 1px solid #dee2e6;
            text-align: left;
        }
        .modal-table th {
            background: #f8f9fa;
            font-weight: 600;
        }
        .validation-list {
            display: flex;
            flex-direction: column;
            gap: 0.75rem;
        }
        .validation-list .validation-item {
            display: flex;
            align-items: flex-start;
            gap: 0.75rem;
            padding: 0.75rem;
            border-radius: 6px;
        }
        .validation-list .validation-item.warning {
            background: rgba(247, 37, 133, 0.05);
            border-left: 3px solid #f72585;
        }
        .validation-list .validation-item.error {
            background: rgba(247, 37, 133, 0.1);
            border-left: 3px solid #f72585;
        }
        .error-message {
            background: rgba(247, 37, 133, 0.1);
            padding: 1rem;
            border-radius: 6px;
            display: flex;
            align-items: flex-start;
            gap: 0.75rem;
            color: #721c24;
        }
    `;
    document.head.appendChild(modalStyles);
});