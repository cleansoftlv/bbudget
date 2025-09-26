using System.ComponentModel.DataAnnotations;

namespace LMApp.Models.Categories
{
    public class BudgetCategoryDisplayForEdit
    {
        public string Name { get; set; }
        public long CategoryId { get; set; }

        public BudgetCategoryType CategoryType { get; set; }


        [Display(Name = "Budget Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Budget amount must be non-negative")]
        public decimal? EditBudgetAmount { get; set; }

        [Display(Name = "Currency")]
        [Required(ErrorMessage = "Currency is required")]
        public string EditCurrency { get; set; }

        public bool IsEditing { get; set; }

        public decimal? OriginalBudgetAmount { get; set; }
        public string OriginalCurrency { get; set; }

        /// <summary>
        /// Creates an editable copy from an existing BudgetCategoryDisplay
        /// </summary>
        public static BudgetCategoryDisplayForEdit FromBudgetCategoryDisplay(
            BudgetCategoryDisplay category,
            string primaryCurrency)
        {
            return new BudgetCategoryDisplayForEdit
            {
                Name = category.Name,
                CategoryId = category.CategoryId,
                CategoryType = category.CategoryType,
                // Edit-specific properties
                EditBudgetAmount = category.Currency == null && category.BudgetedAmount == default ? null : category.BudgetedAmount,
                EditCurrency = category.Currency ?? primaryCurrency,
                IsEditing = false,
                OriginalBudgetAmount = category.Currency == null && category.BudgetedAmount == default ? null : category.BudgetedAmount,
                OriginalCurrency = category.Currency ?? primaryCurrency
            };
        }

        /// <summary>
        /// Checks if any changes have been made
        /// </summary>
        public bool HasChanges()
        {
            return EditBudgetAmount != OriginalBudgetAmount ||
                   !string.Equals(EditCurrency, OriginalCurrency, StringComparison.OrdinalIgnoreCase);
        }

        public void CommitEdit()
        {
            OriginalBudgetAmount = EditBudgetAmount;
            OriginalCurrency = EditCurrency;
            IsEditing = false;
        }

        public void CancelEdit()
        {
            EditBudgetAmount = OriginalBudgetAmount;
            EditCurrency = OriginalCurrency;
            IsEditing = false;
        }


        /// <summary>
        /// Creates an UpsertBudgetRequest from the current edit state
        /// </summary>
        public UpsertBudgetRequest ToUpsertBudgetRequest(DateTime startDate)
        {
            return new UpsertBudgetRequest
            {
                category_id = CategoryId,
                start_date = startDate.ToString("yyyy-MM-dd"),
                amount = EditBudgetAmount.Value,
                currency = EditCurrency.ToLowerInvariant()
            };
        }

    }
}
