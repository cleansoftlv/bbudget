// Google Drive export functionality - ES module for Blazor JS interop

// Exported functions for Blazor JS interop

// Main export function - handles the entire flow
export async function exportToGoogleDrive(request) {
    try {
        // request: { transactions, fileName }
        const accessToken = await getAccessToken(request.oauthClientId);
        const csvData = convertTransactionsToCsv(request.transactions);

        let fileName = request.fileName;
        if (!fileName.endsWith('.csv')) {
            fileName = fileName.replace(/\.[^/.]+$/, '') + '.csv';
        }

        const result = await uploadToGoogleDrive(accessToken, fileName, csvData);

        return {
            success: true,
            fileId: result.fileId,
            webViewLink: result.webViewLink,
            message: 'Successfully exported to Google Sheets!'
        };
    } catch (error) {
        console.error('Google Drive export error:', error);
        return {
            success: false,
            message: error.message
        };
    }
}

// Export raw CSV to Google Drive
export async function exportRawCsvToGoogleDrive(request) {
    try {
        // request: { csvContent, fileName, oauthClientId }
        const accessToken = await getAccessToken(request.oauthClientId);

        let fileName = request.fileName;
        if (!fileName.endsWith('.csv')) {
            fileName = fileName.replace(/\.[^/.]+$/, '') + '.csv';
        }

        const result = await uploadToGoogleDrive(accessToken, fileName, request.csvContent);

        return {
            success: true,
            fileId: result.fileId,
            webViewLink: result.webViewLink,
            message: 'Successfully exported to Google Sheets!'
        };
    } catch (error) {
        console.error('Google Drive export error:', error);
        return {
            success: false,
            message: error.message
        };
    }
}


// Open OAuth popup and get access token
function getAccessToken(oauthClientId) {
    return new Promise((resolve, reject) => {
        if (!oauthClientId) {
            reject(new Error('Google OAuth client ID not configured'));
            return;
        }

        const scope = 'https://www.googleapis.com/auth/drive.file';
        const redirectUri = encodeURIComponent(window.location.origin + '/oauth-callback.html');

        // Ensure client_id is URI encoded
        const encodedClientId = encodeURIComponent(oauthClientId);

        const authUrl = `https://accounts.google.com/o/oauth2/v2/auth?` +
            `client_id=${encodedClientId}&` +
            `redirect_uri=${redirectUri}&` +
            `response_type=token&` +
            `scope=${encodeURIComponent(scope)}&` +
            `prompt=select_account`;

        // Open popup window
        const popup = window.open(
            authUrl,
            'GoogleAuth',
            'width=500,height=600,scrollbars=yes,resizable=yes'
        );

        if (!popup) {
            reject(new Error('Popup blocked. Please allow popups for this site.'));
            return;
        }

        // Listen for messages from popup
        const messageHandler = (event) => {
            if (event.origin !== window.location.origin) {
                return;
            }

            if (event.data.type === 'GOOGLE_AUTH_SUCCESS') {
                window.removeEventListener('message', messageHandler);
                popup.close();
                resolve(event.data.accessToken);
            } else if (event.data.type === 'GOOGLE_AUTH_ERROR') {
                window.removeEventListener('message', messageHandler);
                popup.close();
                reject(new Error(event.data.error || 'Authentication failed'));
            }
        };

        window.addEventListener('message', messageHandler);

        // Check if popup was closed manually
        const checkClosed = setInterval(() => {
            if (popup.closed) {
                clearInterval(checkClosed);
                window.removeEventListener('message', messageHandler);
                reject(new Error('Authentication cancelled'));
            }
        }, 1000);
    });
}

// Convert transactions to CSV format compatible with ExportTransaction class
function convertTransactionsToCsv(transactions) {
    const headers = 'Date,Payee,Account,Amount,Currency,Category,Type,Cleared,Account To,Amount From,Currency From,Amount To,Currency To,Notes,Id,Url\n';
    if (!transactions || transactions.length === 0) {
        return headers;
    }

    const csvRows = transactions.map(transaction => {
        const date = transaction.date || '';
        const payee = escapeCsvField(transaction.payee || '');
        const amount = transaction.amount ?? '';
        const amountTo = transaction.amountTo ?? '';
        const notes = escapeCsvField(transaction.notes || '');
        const transferAmount = transaction.transferAmount ?? '';
        const transferCurrency = escapeCsvField(transaction.transferCurrency ?? '');
        const currency = escapeCsvField(transaction.currency || '');
        const currencyTo = escapeCsvField(transaction.currencyTo || '');
        const account = escapeCsvField(transaction.account || '');
        const accountTo = escapeCsvField(transaction.accountTo || '');
        const category = escapeCsvField(transaction.category || '');
        const type = escapeCsvField(transaction.type || '');
        const cleared = transaction.cleared ? 'Yes' : 'No';
        const id = escapeCsvField(transaction.id || '');
        const url = escapeCsvField(transaction.url || '');

        return `${date},${payee},${account},${amount},${currency},${category},${type},${cleared},${accountTo},${transferAmount},${transferCurrency},${amountTo},${currencyTo},${notes},${id},${url}`;
    });

    return headers + csvRows.join('\n');
}

// Escape CSV field if it contains commas, quotes, or newlines
function escapeCsvField(field) {
    if (typeof field !== 'string') {
        field = String(field || '');
    }

    if (field.includes(',') || field.includes('"') || field.includes('\n') || field.includes('\r')) {
        return `"${field.replace(/"/g, '""')}"`;
    }

    return field;
}

// Get human-readable transaction type
function getTransactionType(tranType) {
    switch (tranType) {
        case 0: return 'Simple';
        case 1: return 'Split';
        case 2: return 'Transfer';
        case 3: return 'Transfer Part';
        default: return 'Unknown';
    }
}

// Create multipart form data for Google Drive API
function createMultipartFormData(fileName, csvData) {
    const boundary = '----WebKitFormBoundary' + Date.now();

    // Metadata JSON part
    const metadata = {
        name: fileName,
        mimeType: 'application/vnd.google-apps.spreadsheet'
    };

    let body = '';
    body += '--' + boundary + '\r\n';
    body += 'Content-Type: application/json\r\n\r\n';
    body += JSON.stringify(metadata) + '\r\n';

    // CSV data part
    body += '--' + boundary + '\r\n';
    body += 'Content-Type: text/csv\r\n\r\n';
    body += csvData + '\r\n';

    // End boundary
    body += '--' + boundary + '--\r\n';

    return {
        body: body,
        boundary: boundary
    };
}

// Make direct API call to Google Drive
async function uploadToGoogleDrive(accessToken, fileName, csvData) {
    try {
        const multipartData = createMultipartFormData(fileName, csvData);

        const response = await fetch('https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${accessToken}`,
                'Content-Type': `multipart/related; boundary=${multipartData.boundary}`
            },
            body: multipartData.body
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.error('Google Drive API error:', response.status, errorText);
            throw new Error(`Google Drive API error: ${response.status} - ${response.statusText}`);
        }

        const result = await response.json();

        return {
            success: true,
            fileId: result.id,
            webViewLink: `https://docs.google.com/spreadsheets/d/${result.id}/edit`
        };
    } catch (error) {
        console.error('Error uploading to Google Drive:', error);
        throw error;
    }
}
