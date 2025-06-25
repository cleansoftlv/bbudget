using LMApp.Models.Account;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LMApp.Models.Transactions
{
    /// <summary>
    /// Helper to calculate running balances for transactions
    /// </summary>
    public static class RunningBalanceCalculator
    {
        /// <summary>
        /// Calculates running balances for transactions in a list
        /// </summary>
        /// <param name="transactions">The transaction list</param>
        /// <param name="accountId">The account ID</param>
        /// <param name="currentBalance">The current account balance</param>
        public static void CalculateRunningBalances(
            List<TransactionDisplay> transactions, 
            decimal currentBalance,
            string accountCurrency)
        {
            if (transactions == null || !transactions.Any())
                return;

            // Make a copy of the transactions list and sort by date (newest first, which is how they're displayed)
            var orderedTransactions = transactions
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .ToList();

            // Working backwards from the current balance
            decimal? runningBalance = currentBalance;

            // For each transaction, working backwards in time
            foreach (var transaction in orderedTransactions)
            {
                if (!string.Equals(transaction.Currency, accountCurrency, StringComparison.InvariantCultureIgnoreCase))
                {
                    // If the transaction is in a different currency, we can't calculate the running balance
                    runningBalance = null;
                }

                // Set the running balance for this transaction
                transaction.RunningBalance = runningBalance;

                // Apply the amount to get the balance after this transaction
                runningBalance += transaction.Amount;
            }
        }
    }
}
