using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Login
{
    public class LMAccountInfo
    {
        public int Id { get; set; }
        public long LMAccountId { get; set; }

        public string Token { get; set; }

        public bool IsActive { get; set; }

        public string Name { get; set; }

        public string UserName { get; set; }

        public LMAccountSettings Settings { get; set; }

        public DateTime LicenseTill { get; set; }

        public bool IsPaidLicense { get; set; }

    }
}
