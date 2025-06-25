using Shared.License;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Login
{
    public class AuthUserInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<LMAccountInfo> Accounts { get; set; }

        public List<SsoInfo> SsoAccounts { get; set; }

        public RequestSignature Signature { get; set; }

    }
}
