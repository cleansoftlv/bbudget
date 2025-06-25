using LMApp.Models.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.UI
{
    public class ForEditPair
    {
        public BaseTransactionForEdit Original { get; set; }
        public BaseTransactionForEdit Updated { get; set; }

        public bool SkipTransferMatchingTransactionCheck { get; set; }

        public BaseTransactionForEdit ConfirmedMatchingTransferTransaction { get; set; }
    }
}
