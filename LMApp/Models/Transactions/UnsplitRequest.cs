using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class UnsplitRequest
    {
        public long[] parent_ids { get; set; }

        public bool remove_parents { get; set; }
    }
}
