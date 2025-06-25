using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.License
{
    public class LicensingInfoResponse
    {
        public List<PaidLicenseInfo> Licenses { get; set; }

        public List<OrderInfo> Orders { get; set; }
    }
}
