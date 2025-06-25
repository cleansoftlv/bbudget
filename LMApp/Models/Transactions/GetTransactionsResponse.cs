using LMApp.Models.Transactions;

public class GetTransactionsResponse: ResponseWithSingleError
{
    public TransactionDto[] transactions { get; set; }

    public bool has_more { get; set; }

}
