using LMApp.Models.Account;
using LMApp.Models.Context;
using System.ComponentModel.DataAnnotations;

namespace LMApp.Models.Transactions
{
    public class AccountTransferTransactionForEdit : BaseTransactionForEdit, IValidatableObject
    {
        override public TransactionType TranType => TransactionType.Transfer;

        [Required]
        [Display(Name = "Amount (sent)")]
        public decimal? AmountFrom { get; set; }

        public decimal? AmountFromWithSign => AmountFrom;
        public string CurrencyFrom { get; set; }
        [Required]
        [Display(Name = "Account From")]

        public string AccountUidFrom { get; set; }

        [Display(Name = "Amount (received)")]
        public decimal? AmountTo { get; set; }

        public decimal? AmoutToWithSign => -AmountTo;

        public string CurrencyTo { get; set; }
        [Required]
        [Display(Name = "Account To")]
        public string AccountUidTo { get; set; }

        public string Notes { get; set; }
        public List<string> Tags { get; set; }


        public bool IsPlaidFrom => TransactionsService.GetAccountTypeByUid(AccountUidFrom) == AccountType.Plaid;
        public bool IsPlaidTo => TransactionsService.GetAccountTypeByUid(AccountUidTo) == AccountType.Plaid;
        public bool IsPlaid => IsPlaidFrom || IsPlaidTo;

        public bool IsPlaidReadonlyFrom =>
            IsPlaidFrom
            && Id != 0
            && (From?.plaid_account_id != null ||
            (Transaction.plaid_account_id.HasValue
                && Transaction.amount >= 0));
        public bool IsPlaidReadonlyTo =>
            IsPlaidTo
            && Id != 0
            && (To?.plaid_account_id != null ||
            (Transaction.plaid_account_id.HasValue
                && Transaction.amount < 0));

        public bool IsPlaidReadonly => IsPlaidReadonlyFrom || IsPlaidReadonlyTo;


        /// <summary>
        /// Determines if AmountTo field should be shown and required (when currencies are different)
        /// </summary>
        public bool ShowAmountTo
        {
            get
            {
                // AmountTo is only required when currencies are different
                return IsCrossCurrency
                       || (Id != 0 && From != null && To != null && From.amount * -1 != To.amount);
            }
        }

        public bool IsCrossCurrency =>
            !string.IsNullOrEmpty(CurrencyFrom) &&
            !string.IsNullOrEmpty(CurrencyTo) &&
            !string.Equals(CurrencyFrom, CurrencyTo, StringComparison.OrdinalIgnoreCase);

        public override bool HasEditChanges => AmountFromWithSign != From?.amount
          || CurrencyFrom != From?.currency
          || AccountUidFrom != From?.AccountUid
          || AmoutToWithSign != To?.amount
          || CurrencyTo != To?.currency
          || AccountUidTo != To?.AccountUid
          || Date != (Transaction == null
            ? null
            : DateTime.ParseExact(Transaction.date, "yyyy-MM-dd", null))
          || Notes != Transaction?.notes;

        public override bool HasChanges =>
            Id <= 0
            || HasEditChanges
            || !IsCleared;

        public override void TrimAll()
        {
            Notes = Notes?.Trim();
        }
        public TransactionChildDto From { get; set; }
        public TransactionChildDto To { get; set; }

        public TransactionChildDto SavedFrom { get; set; }
        public TransactionChildDto SavedTo { get; set; }

        override public TransactionForEditDto[] GetUpdateDtos(
            SettingsService settingsService)
        {
            var fromAccount = settingsService.FindAssetOrPlaidAccount(AccountUidFrom);
            var toAccount = settingsService.FindAssetOrPlaidAccount(AccountUidTo);

            var groupUpdate = Transaction.GetEditDto();

            // Determine which category to use based on whether it's cross-currency
            var isCrossCurrency = !string.IsNullOrEmpty(CurrencyFrom) && 
                                  !string.IsNullOrEmpty(CurrencyTo) && 
                                  !string.Equals(CurrencyFrom, CurrencyTo, StringComparison.OrdinalIgnoreCase);
            
            var categoryId = isCrossCurrency && settingsService.Settings.CrossCurrencyTransferCategoryId.HasValue
                ? settingsService.Settings.CrossCurrencyTransferCategoryId
                : settingsService.Settings.TransferCategoryId;

            groupUpdate.date = Date.ToString("yyyy-MM-dd");
            groupUpdate.notes = Notes;
            groupUpdate.tags = Tags;
            groupUpdate.category_id = categoryId;
            groupUpdate.payee = "From " + (fromAccount?.Name ?? "Unknown") + " to " + (toAccount?.Name ?? "Unknown");
            groupUpdate.status = ClientConstants.TransactionStatusCleared;

            (var fromUpdate, var toUpdate) = GetChildUpdateDtos(settingsService);
            return [fromUpdate, toUpdate, groupUpdate];
        }

        public (TransactionForEditDto from, TransactionForEditDto to) GetChildUpdateDtos(
            SettingsService settingsService)
        {
            var fromAccount = settingsService.FindAssetOrPlaidAccount(AccountUidFrom);
            var toAccount = settingsService.FindAssetOrPlaidAccount(AccountUidTo);

            // Determine which category to use based on whether it's cross-currency
            var isCrossCurrency = !string.IsNullOrEmpty(CurrencyFrom) && 
                                  !string.IsNullOrEmpty(CurrencyTo) && 
                                  !string.Equals(CurrencyFrom, CurrencyTo, StringComparison.OrdinalIgnoreCase);
            
            var categoryId = isCrossCurrency && settingsService.Settings.CrossCurrencyTransferCategoryId.HasValue
                ? settingsService.Settings.CrossCurrencyTransferCategoryId
                : settingsService.Settings.TransferCategoryId;

            var fromUpdate = From.GetEditDto(Transaction);

            fromUpdate.payee = $"To {toAccount?.Name ?? "Unknown"}";
            fromUpdate.amount = AmountFrom ?? 0;
            fromUpdate.currency = CurrencyFrom?.ToLowerInvariant();
            fromUpdate.asset_id = fromAccount?.AssetId;
            fromUpdate.plaid_account_id = fromAccount?.PlaidAccountId;
            fromUpdate.date = Date.ToString("yyyy-MM-dd");
            fromUpdate.notes = Notes;
            fromUpdate.tags = Tags;
            fromUpdate.category_id = categoryId;
            fromUpdate.status = ClientConstants.TransactionStatusCleared;

            var toUpdate = To.GetEditDto(Transaction);

            toUpdate.payee = $"From {fromAccount?.Name ?? "Unknown"}";
            toUpdate.amount = -(AmountTo ?? AmountFrom ?? 0);
            toUpdate.currency = (CurrencyTo ?? CurrencyFrom)?.ToLowerInvariant();
            toUpdate.asset_id = toAccount?.AssetId;
            toUpdate.plaid_account_id = toAccount?.PlaidAccountId;
            toUpdate.date = Date.ToString("yyyy-MM-dd");
            toUpdate.notes = Notes;
            toUpdate.tags = Tags;
            toUpdate.category_id = categoryId;
            toUpdate.status = ClientConstants.TransactionStatusCleared;

            return (fromUpdate, toUpdate);
        }


        public override long? GroupTransactionId => Id;
        public override IdAndAmount[] ChildTransactionIds => [
            new IdAndAmount(From.id, AmountFrom ?? 0),
            new IdAndAmount(To.id, -(AmountTo ?? 0))];

        public override bool TypeCanBeChanged => Id == 0 || From?.plaid_account_id == null || To?.plaid_account_id == null;

        public override string Name => $"Account transfer";

        public override TransactionForInsertDto[] GetInsertDtos(SettingsService settingsService)
        {
            var fromAccount = settingsService.FindAssetOrPlaidAccount(AccountUidFrom);
            var toAccount = settingsService.FindAssetOrPlaidAccount(AccountUidTo);

            // Determine which category to use based on whether it's cross-currency
            var isCrossCurrency = !string.IsNullOrEmpty(CurrencyFrom) && 
                                  !string.IsNullOrEmpty(CurrencyTo) && 
                                  !string.Equals(CurrencyFrom, CurrencyTo, StringComparison.OrdinalIgnoreCase);
            
            var categoryId = isCrossCurrency && settingsService.Settings.CrossCurrencyTransferCategoryId.HasValue
                ? settingsService.Settings.CrossCurrencyTransferCategoryId
                : settingsService.Settings.TransferCategoryId;

            var from = new TransactionForInsertDto
            {
                amount = AmountFrom ?? 0,
                category_id = categoryId,
                asset_id = fromAccount.AssetId,
                plaid_account_id = fromAccount.PlaidAccountId,
                currency = CurrencyFrom?.ToLowerInvariant(),
                date = Date.ToString("yyyy-MM-dd"),
                notes = Notes,
                payee = $"To {toAccount?.Name ?? "Unknown"}",
                tags = Tags,
                external_id = null,
                recurring_id = null,
                status = ClientConstants.TransactionStatusCleared
            };

            var to = new TransactionForInsertDto
            {
                amount = -(AmountTo ?? AmountFrom ?? 0),
                category_id = categoryId,
                asset_id = toAccount?.AssetId,
                plaid_account_id = toAccount?.PlaidAccountId,
                currency = (CurrencyTo ?? CurrencyFrom)?.ToLowerInvariant(),
                date = Date.ToString("yyyy-MM-dd"),
                notes = Notes,
                payee = $"From {fromAccount?.Name ?? "Unknown"}",
                tags = Tags,
                external_id = null,
                recurring_id = null,
                status = ClientConstants.TransactionStatusCleared
            };

            return [from, to];
        }

        public override CreateGroupDto GetGroupDto(SettingsService settingsService, long[] transactionIds)
        {
            var fromAccount = settingsService.FindAssetOrPlaidAccount(AccountUidFrom);
            var toAccount = settingsService.FindAssetOrPlaidAccount(AccountUidTo);

            // Determine which category to use based on whether it's cross-currency
            var isCrossCurrency = !string.IsNullOrEmpty(CurrencyFrom) && 
                                  !string.IsNullOrEmpty(CurrencyTo) && 
                                  !string.Equals(CurrencyFrom, CurrencyTo, StringComparison.OrdinalIgnoreCase);
            
            var categoryId = isCrossCurrency && settingsService.Settings.CrossCurrencyTransferCategoryId.HasValue
                ? settingsService.Settings.CrossCurrencyTransferCategoryId
                : settingsService.Settings.TransferCategoryId;

            return new CreateGroupDto
            {
                transactions = transactionIds,
                date = Date.ToString("yyyy-MM-dd"),
                notes = Notes,
                tags = Tags,
                category_id = categoryId,
                payee = $"From {fromAccount?.Name ?? "Unknown"} to {toAccount?.Name ?? "Unknown"}",
            };
        }



        public override void UpdateWith(BaseTransactionForEdit other, SettingsService service)
        {
            if (other is SimpleTransactionForEdit s)
            {
                Id = s.Id;
                IsCleared = s.IsCleared;
                Transaction = s.Transaction;
                SavedTransaction = s.SavedTransaction;
                Date = s.Date;
                Notes = s.Notes;
                Tags = s.Tags;
                if (s.IsCredit)
                {
                    AmountTo = s.Amount;
                    CurrencyTo = s.Currency;
                    AccountUidTo = s.AccountUid;
                    if (AccountUidFrom == null || CurrencyFrom == CurrencyTo)
                    {
                        AmountFrom = s.Amount;
                        CurrencyFrom = s.Currency;
                    }
                }
                else
                {
                    AmountFrom = s.Amount;
                    CurrencyFrom = s.Currency;
                    AccountUidFrom = s.AccountUid;
                }
            }
            else if (other is AccountTransferTransactionForEdit t)
            {
                Id = t.Id;
                IsCleared = t.IsCleared;
                Transaction = t.Transaction;
                SavedTransaction = t.SavedTransaction;
                AmountFrom = t.AmountFrom;
                AmountTo = t.AmountTo;
                CurrencyFrom = t.CurrencyFrom;
                CurrencyTo = t.CurrencyTo;
                AccountUidFrom = t.AccountUidFrom;
                AccountUidTo = t.AccountUidTo;
                Date = t.Date;
                Notes = t.Notes;
                Tags = t.Tags;
                From = t.From;
                To = t.To;
                SavedFrom = t.SavedFrom;
                SavedTo = t.SavedTo;
            }
            else if (other is SplitTransactionForEdit split)
            {
                Id = split.Id;
                IsCleared = split.IsCleared;
                Transaction = split.Transaction;
                SavedTransaction = split.SavedTransaction;
                Date = split.Date;
                Notes = split.Notes;
                Tags = split.Tags;
                if (split.IsCredit)
                {
                    AmountTo = split.Amount;
                    CurrencyTo = split.Currency;
                    AccountUidTo = split.AccountUid;
                    if (AccountUidFrom == null || CurrencyFrom == CurrencyTo)
                    {
                        AmountFrom = split.Amount;
                        CurrencyFrom = split.Currency;
                    }
                }
                else
                {
                    AmountFrom = split.Amount;
                    CurrencyFrom = split.Currency;
                    AccountUidFrom = split.AccountUid;
                }
            }
        }


        public static AccountTransferTransactionForEdit CreateFromTwo(SimpleTransactionForEdit from, SimpleTransactionForEdit to)
        {
            var minDate = from.Date < to.Date ? from.Date : to.Date;

            return new AccountTransferTransactionForEdit
            {
                Id = 0, // New transaction, so Id is 0
                IsCleared = true,
                Date = minDate,
                Notes = null,
                Tags = from.Tags.Union(to.Tags).ToList(),
                AmountFrom = from.Amount,
                CurrencyFrom = from.Currency,
                AccountUidFrom = from.AccountUid,
                AmountTo = to.Amount,
                CurrencyTo = to.Currency,
                AccountUidTo = to.AccountUid,
                Transaction = null,
                From = TransactionChildDto.FromTransaction(from.Transaction),
                To = TransactionChildDto.FromTransaction(to.Transaction)
            };
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {

            // Only require AmountTo when currencies are different
            if (ShowAmountTo && (!AmountTo.HasValue || AmountTo <= 0))
            {
                yield return new ValidationResult(
                    "Amount (received) is required when currencies are different.",
                    [nameof(AmountTo)]);
            }

            if (IsPlaidFrom && (Id == 0
                || (From != null && From.plaid_account_id == null)
                || (Transaction != null && Transaction.amount >= 0 && Transaction.plaid_account_id == null)
                ))
            {
                yield return new ValidationResult(
                    "Account From - transactions for Plaid synced accounts can't be created manually.",
                    [nameof(AccountUidFrom)]);
            }

            if (IsPlaidTo && (Id == 0 
                || (To!= null && To.plaid_account_id == null)
                || (Transaction != null && Transaction.amount < 0 && Transaction.plaid_account_id == null)
                ))
            {
                yield return new ValidationResult(
                    "Account To - transactions for Plaid synced accounts can't be created manually.",
                    [nameof(AccountUidTo)]);
            }
        }
    }
}
