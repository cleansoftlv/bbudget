using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class TransactionForEditDto : TransactionForEditPlaidDto
    {
        public decimal amount { get; set; }
        public long? asset_id { get; set; }
        public string currency { get; set; }


        public TransactionForEditPlaidDto GetPlaidDto()
        {
            return new TransactionForEditPlaidDto
            {
                id = id,
                date = date,
                payee = payee,
                notes = notes,
                category_id = category_id,
                plaid_account_id = plaid_account_id,
                recurring_id = recurring_id,
                status = status,
                tags = tags
            };
        }
    }
}
