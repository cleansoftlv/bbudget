using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class GetTransactionsResult
    {
        public List<TransactionDisplay> Transactions { get; set; }

        public bool HasMore { get; set; }
    }
}
