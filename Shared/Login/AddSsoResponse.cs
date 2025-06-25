using Services.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Login
{
    public class AddSsoResponse
    {
        public SsoAddResult Response { get; set; }
        public SsoInfo Sso { get; set; }
    }
}
