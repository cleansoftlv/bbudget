using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.License
{
    public static class DeclineReasons
    {
        public const string ThreeDsChallengeAbandoned = "3ds_challenge_abandoned";
        public const string ThreeDsChallengeFailedManually = "3ds_challenge_failed_manually";
        public const string CardholderNameMissing = "cardholder_name_missing";
        public const string CustomerChallengeAbandoned = "customer_challenge_abandoned";
        public const string CustomerChallengeFailed = "customer_challenge_failed";
        public const string CustomerNameMismatch = "customer_name_mismatch";
        public const string DoNotHonour = "do_not_honour";
        public const string ExpiredCard = "expired_card";
        public const string HighRisk = "high_risk";
        public const string InsufficientFunds = "insufficient_funds";
        public const string InvalidAddress = "invalid_address";
        public const string InvalidAmount = "invalid_amount";
        public const string InvalidCard = "invalid_card";
        public const string InvalidCountry = "invalid_country";
        public const string InvalidCvv = "invalid_cvv";
        public const string InvalidEmail = "invalid_email";
        public const string InvalidExpiry = "invalid_expiry";
        public const string InvalidMerchant = "invalid_merchant";
        public const string InvalidPhone = "invalid_phone";
        public const string InvalidPin = "invalid_pin";
        public const string IssuerNotAvailable = "issuer_not_available";
        public const string PickUpCard = "pick_up_card";
        public const string RejectedByCustomer = "rejected_by_customer";
        public const string RestrictedCard = "restricted_card";
        public const string SuspectedFraud = "suspected_fraud";
        public const string TechnicalError = "technical_error";
        public const string TransactionNotAllowedForCardholder = "transaction_not_allowed_for_cardholder";
        public const string UnknownCard = "unknown_card";
        public const string WithdrawalLimitExceeded = "withdrawal_limit_exceeded";

        public static string GetDescription(string reason)
        {
            return reason switch
            {
                ThreeDsChallengeAbandoned => "3D Secure challenge was abandoned.",
                ThreeDsChallengeFailedManually => "3D Secure challenge failed manually.",
                CardholderNameMissing => "Cardholder name is missing.",
                CustomerChallengeAbandoned => "Customer abandoned the challenge.",
                CustomerChallengeFailed => "Customer failed the challenge.",
                CustomerNameMismatch => "Customer name does not match.",
                DoNotHonour => "Transaction was declined with 'Do Not Honour' response.",
                ExpiredCard => "The card has expired.",
                HighRisk => "The transaction was flagged as high risk.",
                InsufficientFunds => "Insufficient funds in the account.",
                InvalidAddress => "The address provided is invalid.",
                InvalidAmount => "The amount specified is invalid.",
                InvalidCard => "The card provided is invalid.",
                InvalidCountry => "The country is invalid for this transaction.",
                InvalidCvv => "The CVV code is invalid.",
                InvalidEmail => "The email address provided is invalid.",
                InvalidExpiry => "The card expiry date is invalid.",
                InvalidMerchant => "The merchant is invalid.",
                InvalidPhone => "The phone number provided is invalid.",
                InvalidPin => "The PIN entered is invalid.",
                IssuerNotAvailable => "The card issuer is not available.",
                PickUpCard => "The card has been flagged for pickup.",
                RejectedByCustomer => "The transaction was rejected by the customer.",
                RestrictedCard => "The card is restricted.",
                SuspectedFraud => "The transaction is suspected of fraud.",
                TechnicalError => "A technical error occurred.",
                TransactionNotAllowedForCardholder => "The transaction is not allowed for the cardholder.",
                UnknownCard => "The card is unknown.",
                WithdrawalLimitExceeded => "The withdrawal limit has been exceeded.",
                _ => "Unknown decline reason."
            };
        }
    }
}
