using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.License
{
    public class PaidLicenseInfo
    {
        public int Id { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        public PaidLicenseStatus Status { get; set; }
    }
}
