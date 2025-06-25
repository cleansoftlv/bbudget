using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.License
{
    public class CreateOrderResponse
    {
        public int Id { get; set; }

        public string CheckoutUrl { get; set; }
    }
}
