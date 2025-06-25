using Shared.License;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Login
{
    public class AddLmAccountResponse: SignedResponse
    {
        public int Id { get; set; }

        public DateTime LicenseTill { get; set; }

        public bool IsPaidLicense { get; set; }
    }
}
