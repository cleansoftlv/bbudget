using LMApp.Models.Categories;
using System.ComponentModel.DataAnnotations;

namespace LMApp.Models.Account
{
    public class AccountDisplayForEdit : AccountDisplay
    {
        [Required]
        [Display(Name = "Name")]
        public new string Name
        {
            get => base.Name;
            set => base.Name = value;
        }

        public bool IsNewAccount { get; set; }
        public decimal OriginalBalance { get; set; }
        public string OriginalName { get; set; }

        public bool IsLiabilityEdit
        {
            get => BudgetService.IsLiability(LMAccountType);
            set
            {
                var currentValue = BudgetService.IsLiability(LMAccountType);
                if (currentValue == value)
                {
                    return;
                }

                bool originalIsLiability = BudgetService.IsLiability(OriginalType);
                if (originalIsLiability == value)
                {
                    LMAccountType = OriginalType;
                    return;
                }

                LMAccountType = value ?
                     LMAccountType.OtherLiability :
                     LMAccountType.Cash;
            }
        }

        public LMAccountType OriginalType { get; set; }
        public string OriginalCurrency { get; set; }

        /// <summary>
        /// Creates an editable copy from an existing AccountDisplay
        /// </summary>
        public static AccountDisplayForEdit FromAccountDisplay(AccountDisplay account)
        {
            return new AccountDisplayForEdit
            {
                IdForType = account.IdForType,
                Name = account.Name,
                Balance = account.Balance,
                Currency = account.Currency,
                AccountType = account.AccountType,
                IsLiability = account.IsLiability,
                LMAccountType = account.LMAccountType,
                BalancePrimaryCurrency = account.BalancePrimaryCurrency,
                AssetsBalance = account.AssetsBalance,
                LiabilitiesBalance = account.LiabilitiesBalance,
                IsReadonly = account.IsReadonly,
                CanHaveTransactions = account.CanHaveTransactions,

                // Edit-specific properties
                IsNewAccount = false,
                OriginalBalance = account.Balance,
                OriginalName = account.Name,
                OriginalCurrency = account.Currency,
                OriginalType = account.LMAccountType
            };
        }

        /// <summary>
        /// Creates a new account template for adding
        /// </summary>
        public static AccountDisplayForEdit CreateNewAccount(string primaryCurrency) => new AccountDisplayForEdit
        {
            IdForType = 0, // Will be set by API
            Name = null,
            Balance = 0,
            Currency = primaryCurrency,
            AccountType = AccountType.Default,
            IsLiability = false,
            LMAccountType = Categories.LMAccountType.Cash,

            // Edit-specific properties
            IsNewAccount = true,
            OriginalBalance = 0,
            OriginalType = LMAccountType.Cash,
            OriginalName = "",
            OriginalCurrency = primaryCurrency
        };

        /// <summary>
        /// Checks if this account has any changes compared to original values
        /// </summary>
        public bool HasChanges()
        {
            if (IsNewAccount) return true;

            return Name != OriginalName ||
                   LMAccountType != OriginalType ||
                   !string.Equals(Currency, OriginalCurrency, StringComparison.InvariantCultureIgnoreCase) ||
                   Balance != OriginalBalance;
        }

        public void TrimAll()
        {
            if (Name != null) Name = Name.Trim();
            if (Currency != null) Currency = Currency.Trim().ToLowerInvariant();
        }
    }
}
