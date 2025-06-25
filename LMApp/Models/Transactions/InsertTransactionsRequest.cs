using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class InsertTransactionsRequest
    {
        public TransactionForInsertDto[] transactions { get; set; }

        public bool apply_rules { get; set; }

        public bool skip_duplicates { get; set; }

        public bool check_for_recurring { get; set; }

        public bool debit_as_negative { get; set; }

        public bool skip_balance_update { get; set; }
    }
}
