using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class InsertTransactionsResponse: ResponseWithErrors
    {
        public long[] ids { get; set; }
    }
}
