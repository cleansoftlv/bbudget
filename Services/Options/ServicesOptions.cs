using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Options
{
    public class ServicesOptions
    {
        public bool IsProudction { get; set; }
        public int TrialLicenseLengthDays { get; set; }
        public string DbConnectionString { get; set; }
        public bool IsAzureSql { get; set; }
        public int? SqlCompatabilityLevel { get; set; }
        public string TokenEncryptSecert { get; set; }
        public JwtSettings Jwt { get; set; }

        public RevolutSettings Revolut { get; set; }
    }
}
