using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class UpdateTransactionWithExtIdRequest
    {
        public TransactionForEditWithExtIdDto transaction { get; set; }

        public Split[] split { get; set; }

        public bool debit_as_negative { get; set; }

        public bool skip_balance_update { get; set; }
    }
}
