using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.License
{
    public class PaymentStatusInfo
    {
        public int OrderId { get; set; }

        public OrderStatus OrderStatus { get; set; }

        public bool CanRetry { get; set; }

        public string CheckoutUrl { get; set; }

        public PaymentStatus? LastPaymentStatus { get; set; }

        public string LastPaymentDeclineReason { get; set; }

        public string LastPaymentBankMessage { get; set; }

        public RequestSignature NewSignature { get; set; }

    }
}
