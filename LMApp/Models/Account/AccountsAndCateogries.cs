using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Account
{
    public class AccountsAndCateogries
    {
        public AccountDto[] Accounts { get; set; }

        public PlaidAccountDto[] PlaidAccounts { get; set; }

        public CryptoAccountDto[] CryptoAccounts { get; set; }

        public CategoryItem[] Categories { get; set; }
    }
}
