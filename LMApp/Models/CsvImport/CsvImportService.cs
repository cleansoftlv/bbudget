using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LMApp.Models.Account;
using LMApp.Models.Context;
using LMApp.Models.Transactions;
using Microsoft.AspNetCore.Components.Forms;

namespace LMApp.Models.CsvImport
{
    public class CsvImportService(TransactionsService transactionsService,
        SettingsService settingsService)
    {
        private readonly TransactionsService _transactionsService = transactionsService;
        private readonly SettingsService _settingsService = settingsService;

        // Required CSV columns
        private readonly string[] RequiredColumns = ["Id", "Amount", "Date", "Payee", "Notes", "Currency", "Account"];

        public class CsvImportResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public IEnumerable<CsvTransactionBase> Transactions { get; set; }
            public int DuplicatesCount { get; set; }
            public int InvalidCount { get; set; }
            public DateTime? MinDate { get; set; }
            public DateTime? MaxDate { get; set; }
        }

        public class CsvTransactionBase
        {
            public string Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
            public string Payee { get; set; }
            public string Notes { get; set; }
            public string Currency { get; set; }
            public string Account { get; set; }
            public bool IsSelected { get; set; } = true;

        }

        public class CsvTransaction : CsvTransactionBase
        {
            public bool IsDuplicate { get; set; }
            public bool IsValid { get; set; } = true;
            public string ValidationError { get; set; }
        }

        public async Task<CsvImportResult> ProcessCsvFile(IBrowserFile file)
        {
            var result = new CsvImportResult();

            try
            {
                // Read file content
                using var reader = new StreamReader(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)); // 10MB max
                var content = await reader.ReadToEndAsync();

                // Parse CSV
                var parseResult = ParseCsvContent(content);
                if (!parseResult.Success)
                {
                    return parseResult;
                }

                // Check for duplicates
                await CheckForDuplicates(parseResult);

                result = parseResult;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error processing file: {ex.Message}";
            }

            return result;
        }

        private CsvImportResult ParseCsvContent(string content)
        {
            var result = new CsvImportResult();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
            {
                result.Success = false;
                result.ErrorMessage = "CSV file is empty or contains only headers";
                return result;
            }

            // Detect delimiter
            char delimiter = DetectDelimiter(lines[0]);

            // Parse headers
            var headers = ParseCsvLine(lines[0], delimiter);
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Length; i++)
            {
                headerMap[headers[i].Trim()] = i;
            }

            // Validate required columns
            var missingColumns = RequiredColumns.Where(col => !headerMap.ContainsKey(col)).ToList();
            if (missingColumns.Count > 0)
            {
                result.Success = false;
                result.ErrorMessage = $"Missing required columns: {string.Join(", ", missingColumns)}";
                return result;
            }

            // Parse data rows
            var transactions = new List<CsvTransaction>();
            var invalidCount = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var values = ParseCsvLine(line, delimiter);
                if (values.Length != headers.Length)
                {
                    invalidCount++;
                    continue;
                }

                var transaction = ParseTransaction(values, headerMap);
                if (transaction.IsValid)
                {
                    transactions.Add(transaction);
                }
                else
                {
                    invalidCount++;
                }
            }

            if (transactions.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No valid transactions found in the file";
                return result;
            }

            // Calculate min and max dates
            result.MinDate = transactions.Min(t => t.Date);
            result.MaxDate = transactions.Max(t => t.Date);
            result.Transactions = transactions;
            result.InvalidCount = invalidCount;
            result.Success = true;

            return result;
        }

        private static char DetectDelimiter(string headerLine)
        {
            // Check for tab first, then comma
            return headerLine.Contains('\t') ? '\t' : ',';
        }

        private static string[] ParseCsvLine(string line, char delimiter)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            values.Add(currentValue.ToString());
            return [.. values];
        }

        private CsvTransaction ParseTransaction(string[] values, Dictionary<string, int> headerMap)
        {
            var transaction = new CsvTransaction();

            try
            {
                // Parse mandatory fields
                var amountStr = values[headerMap["Amount"]].Trim();
                var dateStr = values[headerMap["Date"]].Trim();
                var payee = values[headerMap["Payee"]].Trim();
                var account = values[headerMap["Account"]].Trim();

                // Validate mandatory fields
                if (string.IsNullOrEmpty(amountStr) || string.IsNullOrEmpty(dateStr) ||
                    string.IsNullOrEmpty(payee) || string.IsNullOrEmpty(account))
                {
                    transaction.IsValid = false;
                    transaction.ValidationError = "Missing mandatory fields";
                    return transaction;
                }

                // Parse amount (supports both dot and comma as decimal separator)
                amountStr = amountStr.Replace(",", ".");
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                {
                    transaction.IsValid = false;
                    transaction.ValidationError = $"Invalid amount format: {amountStr}";
                    return transaction;
                }
                transaction.Amount = amount;

                // Parse date (expected format: yyyy-MM-dd)
                if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime date))
                {
                    transaction.IsValid = false;
                    transaction.ValidationError = $"Invalid date format: {dateStr}. Expected yyyy-MM-dd";
                    return transaction;
                }
                transaction.Date = date;

                // Set other fields
                transaction.Payee = payee;
                transaction.Account = account;

                // Parse optional fields
                transaction.Id = values[headerMap["Id"]].Trim();
                transaction.Notes = values[headerMap["Notes"]].Trim();

                var currency = values[headerMap["Currency"]].Trim();
                if (!string.IsNullOrEmpty(currency))
                {
                    // Validate currency format (3 character ISO)
                    if (currency.Length != 3)
                    {
                        transaction.IsValid = false;
                        transaction.ValidationError = $"Invalid currency format: {currency}. Expected 3 character ISO code";
                        return transaction;
                    }
                    transaction.Currency = currency.ToUpperInvariant();
                }

                // Generate ID if missing (MD5 hash of account, date, payee, amount)
                if (string.IsNullOrEmpty(transaction.Id))
                {
                    transaction.Id = GenerateTransactionId(account, date, payee, amount);
                }

                transaction.IsValid = true;
            }
            catch (Exception ex)
            {
                transaction.IsValid = false;
                transaction.ValidationError = $"Parse error: {ex.Message}";
            }

            return transaction;
        }

        private static string GenerateTransactionId(string account, DateTime date, string payee, decimal amount)
        {
            var input = $"{account}|{date:yyyy-MM-dd}|{payee}|{amount}";
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            // Convert to base64 URL-safe string
            return Convert.ToBase64String(hash)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private async Task CheckForDuplicates(CsvImportResult result)
        {
            if (!result.Success || !result.Transactions.Any()) return;

            // Load all transactions in the date range
            var minDate = result.MinDate.Value;
            var maxDate = result.MaxDate.Value;

            var allTransactions = new List<TransactionDisplay>();
            var hasMore = true;
            var offset = 0;

            while (hasMore)
            {
                var loadResult = await _transactionsService.GetAllTransactionsAsync(
                    startDate: minDate,
                    endDate: maxDate,
                    offset: offset);

                if (loadResult.Transactions?.Count > 0)
                {
                    allTransactions.AddRange(loadResult.Transactions);
                    offset += loadResult.Transactions.Count;
                }

                hasMore = loadResult.HasMore;
            }

            // Create a set of existing external IDs for fast lookup
            var existingExternalIds = new HashSet<string>(
                allTransactions
                    .Where(t => !string.IsNullOrEmpty(t.Transaction?.external_id))
                    .Select(t => t.Transaction.external_id),
                StringComparer.OrdinalIgnoreCase);

            // Mark duplicates
            var duplicateCount = 0;
            foreach (var transaction in result.Transactions.Cast<CsvTransaction>())
            {
                if (existingExternalIds.Contains(transaction.Id))
                {
                    transaction.IsDuplicate = true;
                    transaction.IsSelected = false; // Deselect duplicates by default
                    duplicateCount++;
                }
            }

            result.Transactions = result.Transactions.Cast<CsvTransaction>()
                .Where(x => !x.IsDuplicate)
                .ToList();

            result.DuplicatesCount = duplicateCount;
        }

        public async Task<ImportTransactionsResult> ImportSelectedTransactions(
            List<CsvTransactionBase> transactions,
            Dictionary<string, long> accountMapping)
        {
            var result = new ImportTransactionsResult();

            try
            {
                var toImport = transactions;

                if (toImport.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "No transactions selected for import";
                    return result;
                }

                // Convert to TransactionForInsertDto
                var transactionsDto = new List<TransactionForInsertDto>();

                foreach (var csvTrans in toImport)
                {
                    if (!accountMapping.TryGetValue(csvTrans.Account, out var accountId))
                    {
                        result.FailedCount++;
                        continue;
                    }

                    var account = _settingsService.FindAssetOrPlaidAccount(accountId, null);
                    if (account == null)
                    {
                        result.FailedCount++;
                        continue;
                    }

                    var dto = new TransactionForInsertDto
                    {
                        date = csvTrans.Date.ToString("yyyy-MM-dd"),
                        payee = csvTrans.Payee,
                        amount = csvTrans.Amount * -1,
                        currency = csvTrans.Currency.ToLowerInvariant(),
                        notes = csvTrans.Notes,
                        asset_id = accountId,
                        status = "uncleared", // All imported transactions are uncleared
                        external_id = csvTrans.Id,
                        // category_id will be null - to be set manually later
                    };

                    transactionsDto.Add(dto);
                }

                if (transactionsDto.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "No valid transactions to import after account mapping";
                    return result;
                }

                // Import transactions in batches
                const int batchSize = 500;
                var totalImported = 0;

                for (int i = 0; i < transactionsDto.Count; i += batchSize)
                {
                    var batch = transactionsDto.Skip(i).Take(batchSize).ToArray();
                    var insertResult = await _transactionsService.InsertTransactions(batch);

                    if (insertResult?.Length > 0)
                    {
                        totalImported += insertResult.Length;
                    }
                    else
                    {
                        result.FailedCount += batch.Length;
                    }
                }

                result.Success = totalImported > 0;
                result.ImportedCount = totalImported;

                if (!result.Success)
                {
                    result.ErrorMessage = "Failed to import any transactions";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Import error: {ex.Message}";
            }

            return result;
        }

        public class ImportTransactionsResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public int ImportedCount { get; set; }
            public int FailedCount { get; set; }
        }
    }
}
