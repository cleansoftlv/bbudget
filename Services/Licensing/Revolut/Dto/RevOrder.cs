using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Licensing.Revolut.Dto
{
    public class RevOrder
    {
        public string id { get; set; }
        public string token { get; set; }
        public string type { get; set; }
        public string state { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public int amount { get; set; }
        public string currency { get; set; }
        public int outstanding_amount { get; set; }
        public string capture_mode { get; set; }
        public string checkout_url { get; set; }
        public string redirect_url { get; set; }
        public string enforce_challenge { get; set; }

        public RevPayment[] payments { get; set; }
    }
}
