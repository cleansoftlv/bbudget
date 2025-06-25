using Shared.License;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Licensing.Revolut.Dto
{
    public static class PaymentStates
    {
        public const string Pending = "pending";
        public const string AuthenticationChallenge = "authentication_challenge";
        public const string AuthenticationVerified = "authentication_verified";
        public const string AuthorisationStarted = "authorisation_started";
        public const string AuthorisationPassed = "authorisation_passed";
        public const string Authorised = "authorised";
        public const string CaptureStarted = "capture_started";
        public const string Captured = "captured";
        public const string RefundValidated = "refund_validated";
        public const string RefundStarted = "refund_started";
        public const string CancellationStarted = "cancellation_started";
        public const string Declining = "declining";
        public const string Completing = "completing";
        public const string Cancelling = "cancelling";
        public const string Failing = "failing";
        public const string Completed = "completed";
        public const string Declined = "declined";
        public const string SoftDeclined = "soft_declined";
        public const string Cancelled = "cancelled";
        public const string Failed = "failed";

        public static PaymentStatus ToRevPaymentStatus(string state)
        {
            return state switch
            {
                Pending => PaymentStatus.Pending,
                AuthenticationChallenge => PaymentStatus.AuthenticationChallenge,
                AuthenticationVerified => PaymentStatus.AuthenticationVerified,
                AuthorisationStarted => PaymentStatus.AuthorisationStarted,
                AuthorisationPassed => PaymentStatus.AuthorisationPassed,
                Authorised => PaymentStatus.Authorised,
                CaptureStarted => PaymentStatus.CaptureStarted,
                Captured => PaymentStatus.Captured,
                RefundValidated => PaymentStatus.RefundValidated,
                RefundStarted => PaymentStatus.RefundStarted,
                CancellationStarted => PaymentStatus.CancellationStarted,
                Declining => PaymentStatus.Declining,
                Completing => PaymentStatus.Completing,
                Cancelling => PaymentStatus.Cancelling,
                Failing => PaymentStatus.Failing,
                Completed => PaymentStatus.Completed,
                Declined => PaymentStatus.Declined,
                SoftDeclined => PaymentStatus.SoftDeclined,
                Cancelled => PaymentStatus.Cancelled,
                Failed => PaymentStatus.Failed,
                _ => PaymentStatus.Unknown
            };
        }
    }
}
