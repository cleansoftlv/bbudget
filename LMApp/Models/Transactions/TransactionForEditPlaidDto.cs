using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class TransactionForEditPlaidDto
    {
        public long id { get; set; }

        public string date { get; set; }

        public string payee { get; set; }

        public long? category_id { get; set; }

        public long? plaid_account_id { get; set; }

        public long? recurring_id { get; set; }
        public string notes { get; set; }
        public string status { get; set; }
        public List<string> tags { get; set; }
    }
}
