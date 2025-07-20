using LMApp.Models.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class SimpleTransactionForEdit : BaseTransactionForEdit, IValidatableObject
    {

        public SimpleTransactionForEdit()
        {
        }

        override public TransactionType TranType => TransactionType.Simple;


        [Required]
        [Display(Name = "Amount")]
        public decimal? Amount { get; set; }

        public decimal? AmountWithSign => Amount * (IsCredit ? -1 : 1);

        public bool IsCredit { get; set; }

        [Required]
        [Display(Name = "Category")]

        public long? CategoryId { get; set; }

        [Required]
        [Display(Name = "Payee")]

        public string Payee { get; set; }

        public string Currency { get; set; }

        [Required]
        [Display(Name = "Account")]
        public string AccountUid { get; set; }

        public long? AssetId => TransactionsService.ParseTranAccountUid(AccountUid).assetId;

        public bool IsPlaid => TransactionsService.GetAccountTypeByUid(AccountUid) == Account.AccountType.Plaid;

        public bool IsPlaidReadonly =>
                IsPlaid
                && Id != 0
                && (Transaction.plaid_account_id.HasValue
                    || (Transaction.is_group
                            && Transaction.children != null
                            && Transaction.children.Any(x => x.plaid_account_id.HasValue)));
        public string Notes { get; set; }
        public List<string> Tags { get; set; }

        public override bool HasEditChanges => AmountWithSign != Transaction?.amount
           || CategoryId != Transaction?.category_id
           || Payee != Transaction?.payee
           || Date != DateTime.ParseExact(Transaction?.date, "yyyy-MM-dd", null)
           || Currency != Transaction?.currency
           || Notes != Transaction?.notes
           || AccountUid != Transaction?.AccountUid;

        public override void TrimAll()
        {
            Payee = Payee?.Trim();
            Notes = Notes?.Trim();
        }

        public override bool HasChanges =>
             Id <= 0
           || HasEditChanges
           || !IsCleared;


        override public TransactionForEditDto[] GetUpdateDtos(SettingsService settingsService)
        {
            return [GetUpdateDto()];
        }

        public TransactionForEditDto GetUpdateDto()
        {
            var parsed = TransactionsService.ParseTranAccountUid(AccountUid);

            return new TransactionForEditDto
            {
                id = Id,
                amount = AmountWithSign ?? 0,
                category_id = CategoryId,
                asset_id = parsed.assetId,
                plaid_account_id = parsed.plaidAccountId,
                currency = Currency?.ToLowerInvariant(),
                date = Date.ToString("yyyy-MM-dd"),
                notes = Notes,
                payee = Payee,
                tags = Tags,
                external_id = Transaction.external_id,
                recurring_id = Transaction.recurring_id,
                status = ClientConstants.TransactionStatusCleared
            };
        }

        public override long? GroupTransactionId => null;

        public override IdAndAmount[] ChildTransactionIds => Array.Empty<IdAndAmount>();

        public override bool TypeCanBeChanged => true;

        public override string Name => Payee;

        public override TransactionForInsertDto[] GetInsertDtos(SettingsService settingsService)
        {
            var parsed = TransactionsService.ParseTranAccountUid(AccountUid);

            return [new TransactionForInsertDto
            {
                amount = (Amount ?? 0) * (IsCredit ? -1 : 1),
                category_id = CategoryId,
                asset_id = parsed.assetId,
                plaid_account_id = parsed.plaidAccountId,
                currency = Currency?.ToLowerInvariant(),
                date = Date.ToString("yyyy-MM-dd"),
                notes = Notes,
                payee = Payee,
                tags = Tags,
                external_id = null,
                recurring_id = null,
                status = ClientConstants.TransactionStatusCleared
            }];
        }

        public override void UpdateWith(BaseTransactionForEdit other, SettingsService service)
        {
            if (other is SimpleTransactionForEdit s)
            {
                Id = s.Id;
                IsCleared = s.IsCleared;
                Transaction = s.Transaction;
                SavedTransaction = s.SavedTransaction;
                Amount = s.Amount;
                IsCredit = s.IsCredit;
                CategoryId = s.CategoryId;
                Payee = s.Payee;
                Date = s.Date;
                Currency = s.Currency;
                AccountUid = s.AccountUid;
                Notes = s.Notes;
                Tags = s.Tags;
            }
            else if (other is AccountTransferTransactionForEdit t)
            {
                Id = t.Id;
                IsCleared = t.IsCleared;
                Transaction = t.Transaction;
                SavedTransaction = t.SavedTransaction;
                Date = t.Date;
                Notes = t.Notes;
                Tags = t.Tags;


                if (t.AccountUidTo != null &&
                    (AccountUid == t.AccountUidTo
                        || t.AccountUidFrom == null
                        || TransactionsService.GetAccountTypeByUid(t.AccountUidTo) == Account.AccountType.Plaid))
                {
                    Amount = t.AmountTo;
                    IsCredit = true;
                    Currency = t.CurrencyTo;
                    AccountUid = t.AccountUidTo;
                }
                else
                {
                    Amount = t.AmountFrom;
                    IsCredit = false;
                    Currency = t.CurrencyFrom;
                    AccountUid = t.AccountUidFrom;
                }
                var crossCurrency = service.Settings.CrossCurrencyTransferCategoryId.HasValue
                    && !string.IsNullOrEmpty(t.CurrencyFrom) &&
                                   !string.IsNullOrEmpty(t.CurrencyTo) &&
                                   !string.Equals(t.CurrencyFrom, t.CurrencyTo, StringComparison.OrdinalIgnoreCase);
                CategoryId ??=
                   crossCurrency?
                    service.Settings.CrossCurrencyTransferCategoryId :
                    service.Settings.TransferCategoryId;
            }
            else if (other is SplitTransactionForEdit split)
            {
                Id = split.Id;
                IsCleared = split.IsCleared;
                Transaction = split.Transaction;
                SavedTransaction = split.SavedTransaction;
                Amount = split.Amount;
                IsCredit = split.IsCredit;
                Currency = split.Currency;
                AccountUid = split.AccountUid;
                CategoryId = split.Children[0].CategoryId;
                Payee = split.Payee;
                Date = split.Date;
                Notes = split.Notes;
                Tags = split.Tags;
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsPlaid &&
                (Id == 0
                || (Transaction.is_group
                    && (Transaction.children == null
                        || Transaction.children.All(x => x.plaid_account_id == null)))
                || (!Transaction.is_group && Transaction.plaid_account_id == null)))
                yield return new ValidationResult("Transactions for Plaid synced accounts can't be created manually.", [nameof(AccountUid)]);
        }
    }
}
