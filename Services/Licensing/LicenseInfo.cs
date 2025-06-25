using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Licensing
{
    public class LicenseInfo
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public bool IsPaid { get; set; }
    }
}
