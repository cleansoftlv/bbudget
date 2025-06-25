using System;

namespace LMApp.Models.Account
{
    public class PlaidAccountDto
    {
        public long id { get; set; } // Unique identifier of Plaid account

        public DateTime date_linked { get; set; } // Date account was first linked in ISO 8601 format

        public string name { get; set; } // Name of the account. Can be overridden by the user.

        public string display_name { get; set; } // Display name of the account

        public string type { get; set; } // Primary type of account

        public string subtype { get; set; } // Optional subtype name of account

        public string mask { get; set; } // Mask (last 3 to 4 digits of account)

        public string institution_name { get; set; } // Name of institution associated with account

        public string status { get; set; } // Current status of the account within Lunch Money

        public decimal balance { get; set; } // Current balance of the account

        public decimal? to_base { get; set; } // The balance converted to the user's primary currency

        public string currency { get; set; } // Currency of account balance in ISO 4217 format

        public DateTime? balance_last_update { get; set; } // Date balance was last updated in ISO 8601 format

        public decimal? limit { get; set; } // Optional credit limit of the account

        public DateTime? import_start_date { get; set; } // Date of earliest date allowed for importing transactions

        public DateTime? last_import { get; set; } // Timestamp of the last time Lunch Money imported new data

        public DateTime? last_fetch { get; set; } // Timestamp of the last successful check for updated data

        public DateTime? plaid_last_successful_update { get; set; } // Timestamp of the last time Plaid successfully connected
    }
}
