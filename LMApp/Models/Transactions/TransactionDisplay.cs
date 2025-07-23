using LMApp.Models.Account;
using LMApp.Models.UI;

namespace LMApp.Models.Transactions
{
    public class TransactionDisplay
    {
        public long Id { get; set; }

        public decimal Amount { get; set; }

      

        public decimal? RunningBalance { get; set; }

        public string Payee { get; set; }
        public string Notes { get; set; }

        public DateTime Date { get; set; }

        public string Currency { get; set; }

        public string AccountName { get; set; }

        public string AccountUid { get; set; }

        public string DestinationAccountName { get; set; }
        public string CategoryName { get; set; }
        public long? CategoryId { get; set; }

        public DateTime CreatedAt { get; set; }

        public TransactionDto Transaction { get; set; }

        public TransactionChildDto From { get; set; }
        public TransactionChildDto To { get; set; }

        public decimal? TransferBalanceAmount { get; set; }
        public string TransferBalanceCurrency { get; set; }

        public TransactionType TranType { get; set; }

        public bool IsInsideGroup { get; set; }
        public long? GroupId { get; set; }
        public bool IsCrossCurrencyTransfer { get; set; }


        public long? ParentId { get; set; }

        public bool IsCleared { get; set; }

        public TransactionListContext? Context { get; set; }


        public BaseTransactionForEdit GetForEdit()
        {
            switch (TranType)
            {
                case TransactionType.Simple:
                case TransactionType.SplitPart:
                    return new SimpleTransactionForEdit
                    {
                        Id = Id,
                        IsCleared = IsCleared,
                        Amount = Math.Abs(Amount),
                        IsCredit = Amount < 0,
                        CategoryId = Transaction.category_id,
                        Payee = Payee,
                        Date = Date,
                        Currency = Currency,
                        AccountUid = Transaction.AccountUid,
                        Notes = Notes,
                        Tags = Transaction.tags.Select(x => x.name).ToList(),
                        Transaction = Transaction
                    };
                case TransactionType.Transfer:
                    return new AccountTransferTransactionForEdit
                    {
                        Id = Id,
                        IsCleared = IsCleared,
                        AccountUidFrom = From.AccountUid,
                        AmountFrom = Math.Abs(From.amount),
                        CurrencyFrom = From.currency,
                        AccountUidTo = To.AccountUid,
                        AmountTo = Math.Abs(To.amount),
                        CurrencyTo = To.currency,
                        Date = Date,
                        Notes = Notes,
                        Tags = Transaction.tags.Select(x => x.name).ToList(),
                        Transaction = Transaction,
                        From = From,
                        To = To
                    };
                case TransactionType.Split:
                case TransactionType.Other:
                case TransactionType.CategoryTransfer:
                default:
                    return new OtherTransactionForEdit
                    {
                        Id = Id,
                        IsCleared = IsCleared,
                        Transaction = Transaction,
                        Amount = Math.Abs(Amount),
                        CategoryId = Transaction.category_id,
                        Payee = Payee,
                        Date = Date,
                        Currency = Currency,
                        AccountUid = Transaction.AccountUid,
                        Notes = Notes,
                        Tags = Transaction.tags.Select(x => x.name).ToList()
                    };
            }


        }
    }
}
