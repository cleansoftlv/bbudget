using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class Split
    {
        public string payee { get; set; }

        public string date { get; set; }

        public long? category_id { get; set; }

        public string notes { get; set; }

        public decimal amount { get; set; }

    }
}
