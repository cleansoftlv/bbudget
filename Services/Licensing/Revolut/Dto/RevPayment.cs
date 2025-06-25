using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Services.Licensing.Revolut.Dto
{
    public class RevPayment
    {
        public string id { get; set; }
        public string state { get; set; }
        public string decline_reason { get; set; }
        public string bank_message { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string token { get; set; }
        public int amount { get; set; }
        public string currency { get; set; }
        public int settled_amount { get; set; }
        public string settled_currency { get; set; }
        //public object payment_method { get; set; }
        //public object authentication_challenge { get; set; }
        //public object billing_address { get; set; }
        public string risk_level { get; set; }
       // public List<object> fees { get; set; }

    }
}
