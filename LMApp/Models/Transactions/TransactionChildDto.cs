using BootstrapBlazor.Components;
using LMApp.Models;
using LMApp.Models.Account;
using LMApp.Models.Transactions;

public class TransactionChildDto
{
    public long id { get; set; }
    public string payee { get; set; }
    public decimal amount { get; set; }
    public string currency { get; set; }

    public string date { get; set; }

    public string notes { get; set; }
    public long? asset_id { get; set; }

    public long? plaid_account_id { get; set; }

    public decimal to_base { get; set; }

    public string AccountUid => TransactionsService.GetAccountUid(asset_id, plaid_account_id);


    public TransactionForEditDto GetEditDto(TransactionDto parent)
    {
        return new TransactionForEditDto
        {
            id = id,
            date = date,
            amount = amount,
            category_id = parent.category_id,
            payee = payee,
            currency = currency,
            asset_id = asset_id,
            plaid_account_id = plaid_account_id,
            recurring_id = parent.recurring_id,
            notes = notes,
            status = parent.status
        };
    }

    public static TransactionChildDto FromTransaction(TransactionDto transaction)
    {
        return new TransactionChildDto
        {
            id = transaction.id,
            payee = transaction.payee,
            amount = transaction.amount,
            currency = transaction.currency,
            date = transaction.date,
            notes = transaction.notes,
            asset_id = transaction.asset_id,
            plaid_account_id = transaction.plaid_account_id,
            to_base = transaction.to_base
        };
    }

}