namespace LMApp.Models.Categories
{
    public class BudgetCategory
    {
        public string category_name { get; set; }

        public long? category_id { get; set; }

        public string category_group_name { get; set; }

        public long? group_id { get; set; }

        public bool? is_group { get; set; }

        public bool is_income { get; set; }

        public bool exclude_from_budget { get; set; }

        public bool exclude_from_totals { get; set; }

        public bool archived { get; set; }

        public RecurringExpenseList recurring { get; set; }

        public long order { get; set; }

        public string date { get; set; }

        public Dictionary<string, BudgetData> data { get; set; }

        public BudgetCategoryConfig config { get; set; }


        public bool ShowInBudget => !exclude_from_budget
            && !archived
            && is_group != true
            && category_id.HasValue 
            && data != null
            && data.Any(x => x.Value.budget_amount != 0 
                || x.Value.spending_to_base != 0
            || x.Value.num_transactions > 0);

        public BudgetCategoryDisplay GetDisplayItem(string primaryCurrency)
        {
            BudgetData data = this.data.Values.First();
            return new BudgetCategoryDisplay
            {
                CategoryType = is_income ?
                    BudgetCategoryType.Income :
                    BudgetCategoryType.Expense,
                CategoryId = category_id.Value,
                Name = category_name,
                BudgetedAmountPrimary = data.budget_to_base,
                ActualAmountPrimary = is_income ? -data.spending_to_base : data.spending_to_base,
                ProgressAmountPrimary = is_income
                    ? -data.spending_to_base
                    : data.budget_to_base - data.spending_to_base,
                PrimaryCurrency = primaryCurrency,
                Currency = data.budget_currency,
                BudgetedAmount = data.budget_amount,
                ExcludeFromTotals = exclude_from_totals
            };
        }
    }
}