using LMApp.Models.Account;
using LMApp.Models.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Context
{
    public class UnifiedAccountInfo
    {
        public string AccountUid => TransactionsService.GetAccountUid(AssetId, PlaidAccountId);

        public long IdForType { get; set; }
        public string Name { get; set; }

        public string Currency { get; set; }

        public AccountType AccountType { get; set; }

        public long? AssetId => AccountType == AccountType.Default ? IdForType : null;
        public long? PlaidAccountId => AccountType == AccountType.Plaid ? IdForType : null;

    }
}
