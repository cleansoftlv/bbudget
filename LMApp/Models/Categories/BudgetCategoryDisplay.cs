using LMApp.Models.Categories;

public class BudgetCategoryDisplay
{
    public string Name { get; set; }
    public long CategoryId { get; set; }

    public decimal BudgetedAmountPrimary { get; set; }
    public decimal ProgressAmountPrimary { get; set; }
    public decimal ActualAmountPrimary { get; set; }

    public string PrimaryCurrency { get; set; }

    public decimal BudgetedAmount { get; set; }
    public string Currency { get; set; }

    public string BudgetedCurrency => BudgetedInPrimary ? PrimaryCurrency : Currency;

    public bool BudgetedInPrimary => BudgetedAmountPrimary == 0 || string.Equals(Currency, PrimaryCurrency, StringComparison.OrdinalIgnoreCase);

    public decimal ProgressAmount => BudgetedInPrimary
        ? ProgressAmountPrimary
        : Math.Round(ProgressAmountPrimary * BudgetedAmount / BudgetedAmountPrimary, 2);

    public decimal ActualAmount => BudgetedInPrimary 
        ? ActualAmountPrimary
        : Math.Round(ActualAmountPrimary * BudgetedAmount / BudgetedAmountPrimary, 2);

    public void AddAmount(decimal amount)
    {
        ActualAmountPrimary += CategoryType == BudgetCategoryType.Income
            ? -amount : amount;
        ProgressAmountPrimary += -amount;
    }

    public BudgetCategoryType CategoryType { get; set; }

    public double UsedPercent => BudgetedAmountPrimary != 0
        ? Math.Round(Decimal.ToDouble(ActualAmountPrimary / BudgetedAmountPrimary * 100), 0)
        : 0.0;
    public double ProgressPercent => BudgetedAmountPrimary != 0
        ? Math.Round(Decimal.ToDouble(ProgressAmountPrimary / BudgetedAmountPrimary * 100), 0)
        : 0.0;

    public double OverspentPercent => BudgetedAmountPrimary != 0 && ActualAmountPrimary > BudgetedAmountPrimary
        ? Math.Round(Decimal.ToDouble((ActualAmountPrimary - BudgetedAmountPrimary) / BudgetedAmountPrimary * 100), 0)
        : 0.0;

    public void UpdateWith(BudgetCategoryDisplay other)
    {
        BudgetedAmountPrimary = other.BudgetedAmountPrimary;
        ProgressAmountPrimary = other.ProgressAmountPrimary;
        ActualAmountPrimary = other.ActualAmountPrimary;
        PrimaryCurrency = other.PrimaryCurrency;
    }

}