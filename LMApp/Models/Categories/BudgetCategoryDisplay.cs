using LMApp.Models.Categories;

public class BudgetCategoryDisplay
{
    public string Name { get; set; }
    public long CategoryId { get; set; }

    public decimal BudgetedAmount { get; set; }
    public decimal ProgressAmount { get; set; }
    public decimal ActualAmount { get; set; }

    public string Currency { get; set; }

    public void AddAmount(decimal amount)
    {
        ActualAmount += CategoryType == BudgetCategoryType.Income
            ? -amount : amount;
        ProgressAmount += -amount;
    }

    public BudgetCategoryType CategoryType { get; set; }

    public double UsedPercent => BudgetedAmount != 0 
        ? Math.Round(Decimal.ToDouble(ActualAmount / BudgetedAmount * 100),0) 
        : 0.0;
    public double ProgressPercent => BudgetedAmount != 0 
        ? Math.Round(Decimal.ToDouble(ProgressAmount / BudgetedAmount * 100),0) 
        : 0.0;

    public double OverspentPercent => BudgetedAmount != 0 && ActualAmount > BudgetedAmount
        ? Math.Round(Decimal.ToDouble((ActualAmount - BudgetedAmount) / BudgetedAmount * 100), 0)
        : 0.0;

    public void UpdateWith(BudgetCategoryDisplay other)
    {
        BudgetedAmount = other.BudgetedAmount;
        ProgressAmount = other.ProgressAmount;
        ActualAmount = other.ActualAmount;
        Currency = other.Currency;
    }

}