using LMApp.Models.Account;
using LMApp.Models.Categories;
using LMApp.Models.Context;
using LMApp.Models.Transactions;
using System.Diagnostics;
using System.Text;
using System.Transactions;

namespace LMApp.Models.Reports
{
    public class ReportsService
    {
        private readonly TransactionsService _transactionsService;
        private readonly BudgetService _budgetService;
        private readonly SettingsService _settingsService;

        public ReportsService(
            TransactionsService transactionsService,
            BudgetService budgetService,
            SettingsService settingsService)
        {
            _transactionsService = transactionsService;
            _budgetService = budgetService;
            _settingsService = settingsService;

        }

        public async Task<ExpenseReportData> GenerateExpenseReportAsync(
            int monthCount = 6,
            Action<string> progressCallback = null)
        {
            progressCallback?.Invoke("Loading transaction data...");

            var reportData = new ExpenseReportData();
            var currentDate = DateTime.Now.Date;
            var currentMonthStart = new DateTime(currentDate.Year, currentDate.Month, 1);

            reportData.FromMonth = currentMonthStart.AddMonths(-monthCount + 1);
            reportData.ToMonth = currentMonthStart.AddMonths(1).AddDays(-1);

            bool includeCrossCurrencyInBudget = false;

            long? crossCurrencyTransferCategory = _settingsService.Settings.CrossCurrencyTransferCategoryId;
            if (crossCurrencyTransferCategory != null)
            {
                var category = _settingsService.GetCachedCategory(crossCurrencyTransferCategory.Value);
                if (category != null && !category.exclude_from_budget)
                {
                    includeCrossCurrencyInBudget = true;
                }
            }

            // Load transactions for all months
            for (int i = 0; i < monthCount; i++)
            {
                var monthStart = currentMonthStart.AddMonths(-monthCount + 1 + i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                progressCallback?.Invoke($"Processing transactions for {monthStart:yyyy-MM}");

                var monthExpenses = await GetExpensesForMonth(monthStart, monthEnd, includeCrossCurrencyInBudget);
                reportData.Expenses.AddRange(monthExpenses);
            }

            return reportData;
        }

        private async Task<List<MonthlyExpense>> GetExpensesForMonth(DateTime monthStart, DateTime monthEnd, bool includeCrossCurrencyInBudget)
        {
            // First, load ALL transactions for the month
            var allTransactions = new List<TransactionDisplay>();
            var offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                var result = await _transactionsService.GetAllTransactionsAsync(
                    monthStart,
                    monthEnd,
                    offset,
                    null, // status
                    ClientConstants.ReportsTransactionsPageSize);

                if (result.Transactions?.Any() == true)
                {
                    allTransactions.AddRange(result.Transactions);
                    offset += result.Transactions.Count;
                }

                hasMore = result.HasMore;
            }
            // Now filter and process all transactions at once
            var filteredTransactions = allTransactions
                .Where(t => t.TranType != TransactionType.Split
                         && t.TranType != TransactionType.TransferPart
                         && (t.TranType != TransactionType.Transfer || (includeCrossCurrencyInBudget && t.IsCrossCurrencyTransfer))
                         && !t.IsInsideGroup)
                .ToList();

            // Group by category and currency AFTER loading all data
            var groupedExpenses = filteredTransactions
                .GroupBy(t => new
                {
                    t.CategoryId,
                    t.CategoryName,
                    t.Currency
                })
                .Select(g => new MonthlyExpense
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName ?? "Uncategorized",
                    Currency = g.Key.Currency,
                    Month = monthEnd, // Last day of the month
                    Balance = g.Sum(t => t.Transaction.amount)
                })
                .ToList();

            return groupedExpenses;
        }

        public string GenerateExpenseReportCsv(ExpenseReportData reportData)
        {
            var csv = new StringBuilder();

            // Get all unique months and sort them chronologically
            var months = reportData.Expenses
                .Select(e => e.Month)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            // Get all unique currencies
            var currencies = reportData.Expenses
                .Select(e => e.Currency)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Get all unique categories and sort them by name
            var categories = reportData.Expenses
                .Select(e => new { e.CategoryId, e.CategoryName })
                .Distinct()
                .OrderBy(c => c.CategoryName)
                .ToList();

            // Create header row
            csv.Append("Category");
            foreach (var currency in currencies)
            {
                foreach (var month in months)
                {
                    csv.Append($",{currency.ToUpperInvariant()} {month:yyyy-MM}");
                }
            }
            csv.AppendLine();

            // Create data rows - one row per category
            foreach (var category in categories)
            {
                csv.Append($"{EscapeCsvField(category.CategoryName)}");

                foreach (var currency in currencies)
                {
                    foreach (var month in months)
                    {
                        var expense = reportData.Expenses
                            .FirstOrDefault(e => e.CategoryId == category.CategoryId
                                              && e.Month == month
                                              && e.Currency == currency);

                        if (expense != null)
                        {
                            csv.Append($",{expense.Balance:F2}");
                        }
                        else
                        {
                            csv.Append(",0.00"); // Show 0 if no transactions for this category/month/currency
                        }
                    }
                }

                csv.AppendLine();
            }

            return csv.ToString();
        }

        public async Task<BalanceReportData> GenerateBalanceReportAsync(
            int monthCount = 6,
            Action<string> progressCallback = null)
        {
            progressCallback?.Invoke("Loading accounts...");

            // Get all accounts
            var accounts = await _budgetService.LoadAccounts();
            var regularAccounts = accounts
                .Where(a => TransactionsService.AccountTypeCanBeUsedInTransaction(a.AccountType)).ToArray();

            var reportData = new BalanceReportData();
            var currentDate = DateTime.Now.Date;
            var currentMonthStart = new DateTime(currentDate.Year, currentDate.Month, 1);

            reportData.ToMonth = currentMonthStart.AddDays(-1);
            reportData.FromMonth = currentMonthStart.AddMonths(-monthCount);

            // Load all transactions for all months at once, organized by month and account
            var allTransactionsByMonth = await LoadAllTransactionsByMonth(
                currentMonthStart,
                monthCount,
                progressCallback);

            // Calculate balances for each account using the pre-loaded transactions
            foreach (var account in regularAccounts)
            {
                progressCallback?.Invoke($"Calculating balances for account: {account.Name}");

                var monthlyBalances = CalculateMonthlyBalancesFromTransactions(
                    account,
                    currentMonthStart,
                    monthCount,
                    allTransactionsByMonth);

                reportData.Balances.AddRange(monthlyBalances);
            }

            return reportData;
        }
        private async Task<Dictionary<DateTime, List<TransactionDisplay>>> LoadAllTransactionsByMonth(
            DateTime currentMonthStart,
            int monthCount,
            Action<string> progressCallback = null)
        {
            var transactionsByMonth = new Dictionary<DateTime, List<TransactionDisplay>>();

            // Load transactions for current month and all previous months
            for (int i = 0; i < monthCount + 1; i++) // +1 to include current month
            {
                var monthStart = currentMonthStart.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                progressCallback?.Invoke($"Loading all transactions for {monthStart:yyyy-MM}");

                var monthTransactions = await GetAllTransactionsForAllAccountsInMonth(monthStart, monthEnd);
                transactionsByMonth[monthStart] = monthTransactions;
            }

            return transactionsByMonth;
        }

        private decimal CalcRunningBalance(decimal current, string accountUid, IEnumerable<TransactionDisplay> periodTransactions)
        {
            var runningBalance = current;
            var currentMonthTransactions = periodTransactions
                   .Where(t => t.Transaction.AccountUid == accountUid
                        && t.TranType != TransactionType.SplitPart
                        && !t.IsInsideGroup)
                   .ToArray();

            // Subtract current month transactions to get the balance at the end of previous month
            foreach (var transaction in currentMonthTransactions)
            {
                //Plus because transaction amounts for debit retured as positive
                runningBalance += transaction.Transaction.amount;
            }

            var transfersAmounts = periodTransactions
                .Where(t => t.From != null && t.From.AccountUid == accountUid)
                .Select(t => t.From.amount)
                .Concat(
                    periodTransactions
                        .Where(t => t.To != null && t.To.AccountUid == accountUid)
                        .Select(t => t.To.amount))
                .ToArray();

            // Adjust running balance for transfers
            foreach (var transferAmount in transfersAmounts)
            {
                runningBalance += transferAmount;
            }

            return runningBalance;
        }

        private List<MonthlyBalance> CalculateMonthlyBalancesFromTransactions(
            AccountDisplay account,
            DateTime currentMonthStart,
            int monthCount,
            Dictionary<DateTime, List<TransactionDisplay>> allTransactionsByMonth)
        {
            var balances = new List<MonthlyBalance>();
            var runningBalance = account.Balance;

            // First, get current month transactions to calculate previous month's closing balance
            var currentMonthKey = currentMonthStart;
            if (allTransactionsByMonth.ContainsKey(currentMonthKey))
            {
                runningBalance = CalcRunningBalance(runningBalance, account.Uid, allTransactionsByMonth[currentMonthKey]);
            }

            // Now work backwards from previous months
            for (int i = 0; i < monthCount; i++)
            {
                var monthStart = currentMonthStart.AddMonths(-1 - i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // The current runningBalance is the closing balance for this month
                balances.Add(new MonthlyBalance
                {
                    AccountUid = account.Uid,
                    AccountName = account.Name,
                    Currency = account.Currency,
                    Month = monthEnd, // Last day of the month
                    ClosingBalance = runningBalance
                });

                // Get transactions for this month from the pre-loaded data
                if (allTransactionsByMonth.ContainsKey(monthStart))
                {
                    runningBalance = CalcRunningBalance(runningBalance, account.Uid, allTransactionsByMonth[monthStart]);
                }
            }

            // Reverse the list so oldest month comes first  
            balances.Reverse();
            return balances;
        }




        private async Task<List<TransactionDisplay>> GetAllTransactionsForAllAccountsInMonth(
            DateTime monthStart,
            DateTime monthEnd)
        {
            var allTransactions = new List<TransactionDisplay>();
            var offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                var result = await _transactionsService.GetAllTransactionsAsync(
                    monthStart,
                    monthEnd,
                    offset,
                    null, // status
                    ClientConstants.ReportsTransactionsPageSize); // Use 1000 for reports

                if (result.Transactions?.Any() == true)
                {
                    // Convert TransactionDisplay to TransactionDto
                    allTransactions.AddRange(result.Transactions);
                    offset += result.Transactions.Count;
                }

                hasMore = result.HasMore;
            }

            return allTransactions;
        }

        public string GenerateBalanceReportCsv(BalanceReportData reportData)
        {
            var csv = new StringBuilder();

            // Get all unique months and sort them chronologically
            var months = reportData.Balances
                .Select(b => b.Month)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            // Get all unique accounts and sort them by name
            var accounts = reportData.Balances
                .Select(b => new { b.AccountUid, b.AccountName, b.Currency })
                .Distinct()
                .OrderBy(a => a.Currency)
                .ThenBy(a => a.AccountName)
                .ToList();

            // Create header row
            csv.Append("Currency,Account Name");
            foreach (var month in months)
            {
                csv.Append($",{month:yyyy-MM-dd}");
            }
            csv.AppendLine();

            // Create data rows - one row per account
            foreach (var account in accounts)
            {
                csv.Append($"{EscapeCsvField(account.Currency.ToUpperInvariant())},{EscapeCsvField(account.AccountName)}");

                foreach (var month in months)
                {
                    var balance = reportData.Balances
                        .FirstOrDefault(b => b.AccountUid == account.AccountUid && b.Month == month);

                    if (balance != null)
                    {
                        csv.Append($",{balance.ClosingBalance:F2}");
                    }
                    else
                    {
                        csv.Append(","); // Empty cell if no data for this month
                    }
                }

                csv.AppendLine();
            }

            return csv.ToString();
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            // Check if the field contains special characters that require escaping
            bool needsQuoting = field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r');

            if (needsQuoting)
            {
                // Escape any existing double quotes by doubling them
                string escapedField = field.Replace("\"", "\"\"");
                return $"\"{escapedField}\"";
            }

            // If no special characters, still quote for consistency (optional)
            return $"\"{field}\"";
        }

        public async Task<IncomeSpendingReportData> GenerateIncomeSpendingReportAsync(
            int monthCount = 6,
            Action<string> progressCallback = null)
        {
            progressCallback?.Invoke("Loading transaction data...");

            var reportData = new IncomeSpendingReportData();
            var currentDate = DateTime.Now.Date;
            var currentMonthStart = new DateTime(currentDate.Year, currentDate.Month, 1);

            reportData.FromMonth = currentMonthStart.AddMonths(-monthCount + 1);
            reportData.ToMonth = currentMonthStart.AddMonths(1).AddDays(-1);
            reportData.Currency = _settingsService.PrimaryCurrency;    

            // Process each month
            for (int i = 0; i < monthCount; i++)
            {
                var monthStart = currentMonthStart.AddMonths(-monthCount + 1 + i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                progressCallback?.Invoke($"Processing transactions for {monthStart:yyyy-MM}");

                var monthData = await GetIncomeSpendingForMonth(monthStart, monthEnd);
                reportData.MonthlyData.Add(monthData);
            }

            return reportData;
        }

        private async Task<MonthlyIncomeSpending> GetIncomeSpendingForMonth(DateTime monthStart, DateTime monthEnd)
        {
            var monthData = new MonthlyIncomeSpending
            {
                Month = monthStart,
                Income = 0,
                Expenses = 0
            };

            // Load ALL transactions for the month
            var allTransactions = await GetAllTransactionsForAllAccountsInMonth(monthStart, monthEnd);

            // Filter and process transactions
            foreach (var transaction in allTransactions)
            {
                // Skip internal transfers, splits, and transfer parts
                if (transaction.TranType == TransactionType.SplitPart ||
                    transaction.TranType == TransactionType.TransferPart ||
                    transaction.IsInsideGroup)
                {
                    continue;
                }

                // Use the base currency amount (to_base) from the transaction
                var baseAmount = transaction.Transaction.to_base;

                // Determine if it's income or expense based on the is_income flag
                if (transaction.Transaction?.is_income == true)
                {
                    monthData.Income -= baseAmount;
                }
                else if (transaction.Transaction?.exclude_from_totals != true)
                {
                    // Only count as expense if not excluded from budget
                    monthData.Expenses += baseAmount;
                }
            }

            return monthData;
        }

        public string GenerateIncomeSpendingReportCsv(IncomeSpendingReportData reportData)
        {
            var csv = new StringBuilder();

            // Add header row
            csv.AppendLine("Month,Income,Expenses,Balance,Currency");

            // Add data rows (excluding total row as requested)
            foreach (var monthData in reportData.MonthlyData.OrderBy(m => m.Month))
            {
                csv.AppendLine($"{monthData.Month:yyyy-MM},{monthData.Income:F2},{monthData.Expenses:F2},{monthData.Balance:F2},{EscapeCsvField(reportData.Currency.ToUpper())}");
            }

            return csv.ToString();
        }
    }
}