using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class TransactionForInsertDto
    {
        public string date { get; set; }
        public decimal amount { get; set; }
        public long? category_id { get; set; }

        public string payee { get; set; }

        public string currency { get; set; }

        public long? asset_id { get; set; }

        public long? plaid_account_id { get; set; }

        public long? recurring_id { get; set; }
        public string notes { get; set; }
        public string status { get; set; }
        public string external_id { get; set; }
        public List<string> tags { get; set; }

        public TransactionForEditDto GetUpdate(long id)
        {
            return new TransactionForEditDto
            {
                id = id,
                amount = amount,
                category_id = category_id,
                currency = currency,
                date = date,
                notes = notes,
                payee = payee,
                tags = tags,
                external_id = external_id,
                recurring_id = recurring_id,
                status = status,
                asset_id = asset_id,
                plaid_account_id = plaid_account_id
            };
        }

    }
}
