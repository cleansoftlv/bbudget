using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Licensing.Revolut.Dto
{
    public class WebhookEventTypes
    {
        public const string OrderCompleted = "ORDER_COMPLETED";
        public const string OrderAuthorised = "ORDER_AUTHORISED";
        public const string OrderCancelled = "ORDER_CANCELLED";
        public const string OrderPaymentAuthenticated = "ORDER_PAYMENT_AUTHENTICATED";
        public const string OrderPaymentDeclined = "ORDER_PAYMENT_DECLINED";
        public const string OrderPaymentFailed = "ORDER_PAYMENT_FAILED";
        public const string PayoutInitiated = "PAYOUT_INITIATED";
        public const string PayoutCompleted = "PAYOUT_COMPLETED";
        public const string PayoutFailed = "PAYOUT_FAILED";
        public const string DisputeActionRequired = "DISPUTE_ACTION_REQUIRED";
        public const string DisputeUnderReview = "DISPUTE_UNDER_REVIEW";
        public const string DisputeWon = "DISPUTE_WON";
        public const string DisputeLost = "DISPUTE_LOST";
    }
}
