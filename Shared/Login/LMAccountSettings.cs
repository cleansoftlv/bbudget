using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Login
{
    public class LMAccountSettings
    {
        public string[] AdditionalCurrencies { get; set; }

        public long? TransferCategoryId { get; set; }

        public long? CrossCurrencyTransferCategoryId { get; set; }

        public bool SortTransactionOnLoadMore { get; set; }
    }
}
