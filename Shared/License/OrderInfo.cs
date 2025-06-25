using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Shared.License
{
    public class OrderInfo
    {
        public int Id { get; set; }

        public OrderStatus Status { get; set; }

        public int ProductDays { get; set; }

        public DateTime CreatedUtc { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; }
    }
}
