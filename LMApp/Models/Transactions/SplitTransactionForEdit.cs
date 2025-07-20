using LMApp.Models.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class SplitTransactionForEdit : BaseTransactionForEdit, IValidatableObject
    {

        public SplitTransactionForEdit()
        {
        }

        public static SplitTransactionForEdit CreateNewForEdit()
        {
            var res = new SplitTransactionForEdit();
            res.OriginalChildTransactions = Array.Empty<TransactionDto>();
            res.Children = new List<SimpleTransactionForEdit>
            {
                new SimpleTransactionForEdit(),
                new SimpleTransactionForEdit()
            };
            return res;
        }

        override public TransactionType TranType => TransactionType.Split;

        [Required]
        [Display(Name = "Amount")]
        public decimal? Amount { get; set; }

        public decimal? AmountWithSign => Amount * (IsCredit ? -1 : 1);


        public bool IsCredit { get; set; }

        [Required]
        [Display(Name = "Payee")]
        public string Payee { get; set; }

        [Required]
        [Display(Name = "Account")]
        public string AccountUid { get; set; }

       

        public string Currency { get; set; }

        public string Notes { get; set; }
        public List<string> Tags { get; set; }

        public List<SimpleTransactionForEdit> Children { get; set; }

        public TransactionDto[] OriginalChildTransactions { get; set; }

        public decimal LastChildAmount => Math.Abs((Amount ?? 0) - Children.Take(Children.Count - 1).Sum(c => c.Amount ?? 0));

        public bool IsLastChildCredit => IsCredit;
        public bool IsPlaid => TransactionsService.GetAccountTypeByUid(AccountUid) == Account.AccountType.Plaid;

        public bool IsPlaidReadonly => IsPlaid
                && Id != 0
                && (Transaction.plaid_account_id.HasValue
                    || (Transaction.is_group
                            && Transaction.children != null
                            && Transaction.children.Any(x => x.plaid_account_id.HasValue)));

        override public TransactionForEditDto[] GetUpdateDtos(SettingsService settingsService)
        {
            return [GetUpdateDto()];
        }

        public TransactionForEditDto GetUpdateDto()
        {
            var parsedUid = TransactionsService.ParseTranAccountUid(AccountUid);

            return new TransactionForEditDto
            {
                id = Id,
                amount = AmountWithSign ?? 0,
                category_id = null,
                currency = Currency,
                date = Date.ToString("yyyy-MM-dd"),
                notes = Notes,
                payee = Payee,
                tags = Tags,
                external_id = null,
                recurring_id = null,
                status = ClientConstants.TransactionStatusCleared,
                asset_id = parsedUid.assetId,
                plaid_account_id = parsedUid.plaidAccountId
            };
        }

        public bool HasParentEditChanges => AmountWithSign != Transaction.amount
            || Payee != Transaction.payee
            || Date != DateTime.ParseExact(Transaction.date, "yyyy-MM-dd", null)
            || Currency != Transaction.currency
            || Notes != Transaction.notes
            || AccountUid != Transaction.AccountUid;


        public override bool HasEditChanges => HasParentEditChanges
            || Children.Any(x => x.HasEditChanges)
            || OriginalChildTransactions == null
            || OriginalChildTransactions.Length != Children.Count;

        public override bool HasChanges =>
            Id <= 0
            || HasParentEditChanges
            || !IsCleared
            || Children.Any(x => x.HasChanges)
            || OriginalChildTransactions == null
            || OriginalChildTransactions.Length != Children.Count;

        public override void TrimAll()
        {
            Payee = Payee?.Trim();
            Notes = Notes?.Trim();
            foreach (var c in Children)
            {
                c.TrimAll();
            }
        }
        public override long? GroupTransactionId => null;
        public override bool TypeCanBeChanged => true;

        public override IdAndAmount[] ChildTransactionIds => Children.Select(x =>
            new IdAndAmount(x.Id, (x.Amount ?? 0) * (x.IsCredit ? -1 : 1))).ToArray();

        public override string Name => Payee;

        public override TransactionForInsertDto[] GetInsertDtos(SettingsService settingsService)
        {
            var parsedUid = TransactionsService.ParseTranAccountUid(AccountUid);

            return [new TransactionForInsertDto
            {
                asset_id = parsedUid.assetId,
                plaid_account_id = parsedUid.plaidAccountId,
                amount = (Amount ?? 0) * (IsCredit ? -1 : 1),
                category_id = null,
                currency = Currency,
                date = Date.ToString("yyyy-MM-dd"),
                notes = Notes,
                payee = Payee,
                tags = Tags,
                external_id = null,
                recurring_id = null,
                status = ClientConstants.TransactionStatusCleared,
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
                AccountUid = s.AccountUid;
                Amount = s.Amount;
                IsCredit = s.IsCredit;
                Payee = s.Payee;
                Date = s.Date;
                Currency = s.Currency;
                Notes = s.Notes;
                Tags = s.Tags;
                Children[0].CategoryId = s.CategoryId;
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
                Children[0].CategoryId ??=
                   crossCurrency ?
                    service.Settings.CrossCurrencyTransferCategoryId :
                    service.Settings.TransferCategoryId;
            }
            else if (other is SplitTransactionForEdit split)
            {
                Id = split.Id;
                IsCleared = split.IsCleared;
                AccountUid = split.AccountUid;
                Transaction = split.Transaction;
                SavedTransaction = split.SavedTransaction;
                Amount = split.Amount;
                IsCredit = split.IsCredit;
                Payee = split.Payee;
                Date = split.Date;
                Currency = split.Currency;
                Notes = split.Notes;
                Tags = split.Tags;
                OriginalChildTransactions = split.OriginalChildTransactions;
                Children = new List<SimpleTransactionForEdit>(split.Children.Count);
                for (int i = 0; i < split.Children.Count; i++)
                {
                    Children.Add(new SimpleTransactionForEdit());
                    Children[i].UpdateWith(split.Children[i], service);
                }
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Children.Any(x => x.CategoryId == null))
            {
                yield return new ValidationResult("All split parts must have a category", [nameof(Children)]);
            }

            if (IsPlaid &&
                 (Id == 0
                 || (Transaction.is_group
                     && (Transaction.children == null
                         || Transaction.children.All(x => x.plaid_account_id == null)))
                 || (!Transaction.is_group && Transaction.plaid_account_id == null)))
            {
                yield return new ValidationResult("Transactions for Plaid synced accounts can't be created manually.", [nameof(AccountUid)]);
            }
        }
    }
}
