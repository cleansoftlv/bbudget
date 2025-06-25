using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Account
{
    public class AccountDto
    {
        public long id { get; set; } // Unique identifier for asset

        public string type_name { get; set; } // Primary type of the asset
                                              // Must be one of: cash, credit, investment, real estate, loan, vehicle, cryptocurrency, employee compensation, other liability, other asset

        public string subtype_name { get; set; } // Optional asset subtype
                                                  // Examples include: retirement, checking, savings, prepaid credit card

        public string name { get; set; } // Name of the asset

        public string display_name { get; set; } // Display name of the asset (as set by user)

        public decimal balance { get; set; } // Current balance of the asset in numeric format to 4 decimal places

        public decimal? to_base { get; set; }
        public DateTime? balance_as_of { get; set; } // Date/time the balance was last updated in ISO 8601 extended format

        public DateTime? closed_on { get; set; } // The date this asset was closed (optional)

        public string currency { get; set; } // Three-letter lowercase currency code of the balance in ISO 4217 format

        public string institution_name { get; set; } // Name of institution holding the asset

        public bool exclude_transactions { get; set; } // If true, this asset will not show up as an option for assignment when creating transactions manually

        public DateTime created_at { get; set; } // Date/time the asset was created in ISO 8601 extended format
    }
}
