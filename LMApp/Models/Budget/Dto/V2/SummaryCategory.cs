using BootstrapBlazor.Components;
using LMApp.Models.Account;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LMApp.Models.Budget.Dto.V2
{
    public class SummaryCategory
    {
        public long category_id { get; set; }

        public CategoryTotals totals { get; set; }

        public List<CategoryOccurrence> occurrences { get; set; }



        public bool ShowInBudget(CategoryItem category) =>
            category != null
              && !category.exclude_from_budget
              && category.archived != true
              && category.is_group != true
              && totals != null
              && (totals.budgeted != null || totals.other_activity != 0 || totals.recurring_activity != 0);


        public BudgetCategoryDisplay GetDisplayItem(string primaryCurrency, CategoryItem category)
        {
            var speningInBaseCur = totals.other_activity + totals.recurring_activity;
            var budgetedInBaseCur = totals.budgeted ?? 0;

            decimal budgetedAmount = budgetedInBaseCur;
            string budgetCurrency = null;
            if (occurrences != null 
                && occurrences.Count == 1)
            {
                var occurrence = occurrences[0];
                if (occurrence.budgeted_amount != null)
                {
                    budgetCurrency = occurrence.budgeted_currency;
                    budgetedAmount = occurrence.budgeted_amount.Value;
                }
            }

            if (budgetCurrency == null)
            {
                budgetedAmount = budgetedInBaseCur;
                budgetCurrency = primaryCurrency;
            }

           
            return new BudgetCategoryDisplay
            {
                CategoryType = category.is_income ?
                    BudgetCategoryType.Income :
                    BudgetCategoryType.Expense,
                CategoryId = category_id,
                Name = category.name,
                BudgetedAmountPrimary = budgetedInBaseCur,
                ActualAmountPrimary = category.is_income ? -speningInBaseCur : speningInBaseCur,
                ProgressAmountPrimary = category.is_income
                    ? -speningInBaseCur
                    : budgetedInBaseCur - speningInBaseCur,
                PrimaryCurrency = primaryCurrency,
                Currency = budgetCurrency,
                BudgetedAmount = budgetedAmount,
                ExcludeFromTotals = category.exclude_from_totals
            };
        }
    }
}
