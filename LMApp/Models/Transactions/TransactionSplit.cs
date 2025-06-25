using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class TransactionSplit
    {
        public TransactionDisplay Parent { get; set; }

        public List<TransactionDisplay> Children { get; set; }
    }
}
