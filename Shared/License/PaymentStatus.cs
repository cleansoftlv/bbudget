using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.License
{
    public enum PaymentStatus
    {
        Pending,
        AuthenticationChallenge,
        AuthenticationVerified,
        AuthorisationStarted,
        AuthorisationPassed,
        Authorised,
        CaptureStarted,
        Captured,
        RefundValidated,
        RefundStarted,
        CancellationStarted,
        Declining,
        Completing,
        Cancelling,
        Failing,
        Completed,  
        Declined,
        SoftDeclined,
        Cancelled,
        Failed,
        Unknown
    }
}
