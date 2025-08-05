using LMApp.Models.Reports;
using LMApp.Models.Transactions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text;

namespace LMApp.Models.UI.Csv
{
    public class CsvExporter
    {
        private readonly Utils _utils;
        private readonly ReportsService _reportService;
        private readonly NavigationManager _navigationManager;
        private readonly UserContextService _userContextService;

        public CsvExporter(Utils utils, 
            ReportsService reportService,
            NavigationManager navigationManager,
            UserContextService userContextService
            )
        {
            _utils = utils;
            _reportService = reportService;
            _userContextService = userContextService;
            _navigationManager = navigationManager;
        }

        /// <summary>
        /// Exports transactions to CSV file and triggers download
        /// </summary>
        /// <param name="transactions">List of transactions to export</param>
        /// <param name="fileName">Name of the file (will auto-append .csv if needed)</param>
        /// <returns>Task representing the async operation</returns>
        public async Task ExportToCsvAsync(IEnumerable<TransactionDisplay> transactions, string fileName)
        {
            if (transactions == null || !transactions.Any())
            {
                throw new ArgumentException("No transactions to export", nameof(transactions));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be empty", nameof(fileName));
            }

            // Generate CSV content
            var csvContent = GenerateCsvContent(transactions);

            // Ensure filename has .csv extension
            var normalizedFileName = NormalizeFileName(fileName);

            // Trigger download
            await _utils.DownloadFile(normalizedFileName, csvContent, "text/csv");
        }

        /// <summary>
        /// Converts transactions to CSV format string
        /// </summary>
        /// <param name="transactions">Transactions to convert</param>
        /// <returns>CSV formatted string</returns>
        public string GenerateCsvContent(IEnumerable<TransactionDisplay> transactions)
        {
            if (transactions == null || !transactions.Any())
            {
                throw new ArgumentException("No transactions to export", nameof(transactions));
            }

            var csv = new StringBuilder();

            // Add header row
            csv.AppendLine("Date,Payee,Account,Amount,Currency,Category,Type,Cleared,Account To,Amount From,Currency From,Amount To,Currency To,Notes,Id,Link");

            var origin = _navigationManager.BaseUri.TrimEnd('/');
            // Add data rows
            foreach (var transaction in transactions)
            {
                var exportTransaction = _reportService.ConvertTranToExportFormat(transaction, _userContextService.CurrentAccount.AccountId, origin);
                AppendCsvRow(exportTransaction, csv);
            }

            return csv.ToString();
        }

        

        /// <summary>
        /// Formats a single transaction as a CSV row
        /// </summary>
        /// <param name="transaction">Transaction to format</param>
        /// <returns>CSV formatted row string</returns>
        private void AppendCsvRow(ExportTransaction transaction, StringBuilder sb)
        {
            var fields = new string[]
            {
                transaction.Date,
                EscapeCsvField(transaction.Payee),
                EscapeCsvField(transaction.Account),
                transaction.Amount.ToString("F2"),
                EscapeCsvField(transaction.Currency),
                EscapeCsvField(transaction.Category),
                EscapeCsvField(transaction.Type),
                transaction.Cleared ? "Yes" : "No",
                EscapeCsvField(transaction.AccountTo),
                transaction.TransferAmount?.ToString("F2") ?? "",
                EscapeCsvField(transaction.TransferCurrency),
                transaction.AmountTo?.ToString("F2") ?? "",
                EscapeCsvField(transaction.CurrencyTo),
                EscapeCsvField(transaction.Notes),
                transaction.Id.ToString(),
                EscapeCsvField(transaction.Url)
            };
            sb.AppendJoin(",", fields);
            sb.AppendLine();
        }

        /// <summary>
        /// Escapes CSV field content by wrapping in quotes and escaping internal quotes
        /// </summary>
        /// <param name="field">Field content to escape</param>
        /// <returns>Escaped field content safe for CSV</returns>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // If field contains comma, quote, or newline, wrap in quotes and escape internal quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                var escapedField = field.Replace("\"", "\"\"");
                return $@"""{escapedField}""";
            }

            return field;
        }

        /// <summary>
        /// Normalizes filename by ensuring .csv extension
        /// </summary>
        /// <param name="fileName">Original filename</param>
        /// <returns>Normalized filename with .csv extension</returns>
        private string NormalizeFileName(string fileName)
        {
            if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }

            // Remove existing extension if present
            var extension = Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(extension))
            {
                fileName = fileName.Replace(extension, "");
            }

            return fileName + ".csv";
        }
    }
}
