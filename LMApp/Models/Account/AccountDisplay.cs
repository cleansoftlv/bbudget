using LMApp.Models.Categories;
using LMApp.Models.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Account
{
    public class AccountDisplay
    {
        public long IdForType { get; set; }

        public string Uid
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(AccountType switch
                {
                    AccountType.Default => TransactionsService.AssetAccountIdPrefix,
                    AccountType.Plaid => TransactionsService.PlaidAccountIdPrefix,
                    AccountType.Total => TransactionsService.TotalAccountIdPrefix,
                    AccountType.Crypto => TransactionsService.CryptoAccountIdPrefix,
                    _ => TransactionsService.OtherAccountIdPrefix
                });
                sb.Append(IdForType);
                if (AccountType == AccountType.Crypto)
                {
                    sb.Append(Name);
                    sb.Append(Currency);
                }
                return sb.ToString();
            }
        }
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; }
        public AccountType AccountType { get; set; }
        public bool IsLiability { get; set; }

        public LMAccountType LMAccountType { get; set; }

        public decimal BalancePrimaryCurrency { get; set; }

        public decimal? AssetsBalance { get; set; }
        public decimal? LiabilitiesBalance { get; set; }

        public bool IsReadonly { get; set; }

        public bool CanHaveTransactions { get; set; }

        public void UpdateWith(AccountDisplay other)
        {
            if (other == null) return;
            Name = other.Name;
            Balance = other.Balance;
            Currency = other.Currency;
            AccountType = other.AccountType;
            BalancePrimaryCurrency = other.BalancePrimaryCurrency;
            LMAccountType = other.LMAccountType;
            IsLiability = other.IsLiability;

        }

        public void AddAmount(decimal amount)
        {
            Balance += amount;
            if (IsLiability && LiabilitiesBalance.HasValue)
            {
                LiabilitiesBalance += amount;
            }
            if (!IsLiability && AssetsBalance.HasValue)
            {
                AssetsBalance += amount;
            }
        }

        public void AddAmountToTotalAccount(decimal amount, bool addedToLiabilityAccount)
        {
            Balance += amount;
            if (addedToLiabilityAccount && LiabilitiesBalance.HasValue)
            {
                LiabilitiesBalance += amount;
            }
            if (!addedToLiabilityAccount && AssetsBalance.HasValue)
            {
                AssetsBalance += amount;
            }
        }
    }
}
