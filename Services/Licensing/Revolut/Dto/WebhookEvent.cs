using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Licensing.Revolut.Dto
{
    public class OrderWebhookEvent
    {
        public string @event { get; set; }
        public string order_id { get; set; }
        public string merchant_order_ext_ref { get; set; }
    }
}
