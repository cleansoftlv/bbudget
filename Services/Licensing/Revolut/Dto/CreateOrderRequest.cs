using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Licensing.Revolut.Dto
{
    public class CreateOrderRequest
    {
        public int amount { get; set; }
        public string currency { get; set; }
        public string capture_mode { get; set; } = "automatic";
        public string cancel_authorised_after { get; set; }
        public string description { get; set; }
        public string enforce_challenge { get; set; }
        public Dictionary<string,string> metadata { get; set; }
        public MerchantOrderData merchant_order_data { get; set; }
        public string redirect_url { get; set; }
        public string statement_descriptor_suffix { get; set; }
        public string settlement_currency { get; set; }
    }
}
