using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public enum TransactionType
    {
        Simple, Transfer, Split, Other, CategoryTransfer, SplitPart, TransferPart
    }
}
