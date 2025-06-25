using System;

namespace LMApp.Models.Account
{
    public class CryptoAccountDto
    {
        public long? id { get; set; } // Unique identifier for a manual crypto account (null for synced accounts)

        public long? zabo_account_id { get; set; } // Unique identifier for a synced crypto account (null for manual accounts)

        public string source { get; set; } // "synced" or "manual"

        public string name { get; set; } // Name of the crypto asset

        public string display_name { get; set; } // Display name of the crypto asset (as set by user)

        public decimal balance { get; set; } // Current balance

        public DateTime? balance_as_of { get; set; } // Date/time the balance was last updated

        public string currency { get; set; } // Abbreviation for the cryptocurrency

        public string status { get; set; } // The current status of the crypto account ("active" or "error")

        public string institution_name { get; set; } // Name of provider holding the asset

        public DateTime? created_at { get; set; } // Date/time the asset was created

        public decimal? to_base { get; set; } // The balance converted to the user's primary currency
    }
}
