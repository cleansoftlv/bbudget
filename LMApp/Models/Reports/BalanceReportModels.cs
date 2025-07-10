using System;
using System.Collections.Generic;
using System.Linq;

namespace LMApp.Models.Reports
{
    public class MonthlyBalance
    {
        public string AccountUid { get; set; }
        public string AccountName { get; set; }
        public string Currency { get; set; }
        public DateTime Month { get; set; }
        public decimal ClosingBalance { get; set; }
    }

    public class BalanceReportData
    {
        public List<MonthlyBalance> Balances { get; set; } = new List<MonthlyBalance>();
        public DateTime FromMonth { get; set; }
        public DateTime ToMonth { get; set; }
    }

    public class MonthlyExpense
    {
        public long? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Currency { get; set; }
        public DateTime Month { get; set; }
        public decimal Balance { get; set; }
    }

    public class ExpenseReportData
    {
        public List<MonthlyExpense> Expenses { get; set; } = new List<MonthlyExpense>();
        public DateTime FromMonth { get; set; }
        public DateTime ToMonth { get; set; }
    }

    public class MonthlyIncomeSpending
    {
        public DateTime Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expenses { get; set; }
        public decimal Balance => Income - Expenses;
    }

    public class IncomeSpendingReportData
    {
        public List<MonthlyIncomeSpending> MonthlyData { get; set; } = new List<MonthlyIncomeSpending>();
        public DateTime FromMonth { get; set; }
        public DateTime ToMonth { get; set; }

        public string Currency { get; set; }


        public decimal TotalIncome => MonthlyData.Sum(m => m.Income);
        public decimal TotalExpenses => MonthlyData.Sum(m => m.Expenses);
        public decimal TotalBalance => TotalIncome - TotalExpenses;
    }
}
