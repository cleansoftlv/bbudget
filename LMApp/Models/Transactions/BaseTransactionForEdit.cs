using LMApp.Models.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public abstract class BaseTransactionForEdit
    {
        public long Id { get; set; }
        public bool IsCleared { get; set; }

        [Required]
        [Display(Name = "Date")]
        public DateTime Date { get; set; }

        public abstract bool HasEditChanges
        {
            get;
        }

        public abstract bool HasChanges { get; }

        public abstract bool TypeCanBeChanged { get; }

        public abstract TransactionType TranType { get; }
        public abstract TransactionForEditDto[] GetUpdateDtos(
            SettingsService settingsService);
        public abstract TransactionForInsertDto[] GetInsertDtos(
            SettingsService settingsService);
        public virtual CreateGroupDto GetGroupDto(
           SettingsService settingsService,
            long[] transactionIds)
        {
            return null;
        }

        public abstract long? GroupTransactionId { get; }
        public abstract IdAndAmount[] ChildTransactionIds { get; }

        public TransactionDto Transaction { get; set; }
        public TransactionDto SavedTransaction { get; set; }

        public abstract void UpdateWith(BaseTransactionForEdit other, SettingsService service);


        public abstract void TrimAll();

        public BaseTransactionForEdit CopyForNew(SettingsService service)
        {
            BaseTransactionForEdit res;

            res = this switch
            {
                SimpleTransactionForEdit => new SimpleTransactionForEdit(),
                SplitTransactionForEdit => new SplitTransactionForEdit(),
                AccountTransferTransactionForEdit => new AccountTransferTransactionForEdit(),
                _ => throw new NotSupportedException($"Unsupported transaction type: {this.GetType().Name}")
            };
            res.UpdateWith(this, service);
            res.Transaction = null; // Clear the transaction to create a new one
            res.SavedTransaction = null; // Clear the saved transaction to create a new one
            res.Id = 0; // Reset the ID for a new transaction
            if (res is SplitTransactionForEdit split)
            {
                split.OriginalChildTransactions = Array.Empty<TransactionDto>(); 
                foreach (var child in split.Children)
                {
                    child.Id = 0; // Reset the ID for each child transaction
                    child.Transaction = null; // Clear the transaction to create a new one
                    child.SavedTransaction = null; // Clear the saved transaction to create a new one
                }
            }
            else if (res is AccountTransferTransactionForEdit transfer)
            {
                transfer.From = null;
                transfer.To = null;
                transfer.SavedFrom = null;
                transfer.SavedTo = null;
            }
            return res;
        }
    
        public void ClearAmount()
        {
            if (this is SimpleTransactionForEdit simple)
            {
                simple.Amount = null;
            }
            else if (this is SplitTransactionForEdit split)
            {
                split.Amount = null;
                foreach (var child in split.Children)
                {
                    child.Amount = null;
                }
            }
            else if (this is AccountTransferTransactionForEdit transfer)
            {
                transfer.AmountFrom = null;
                transfer.AmountTo = null;
            }
        }
    }
}
