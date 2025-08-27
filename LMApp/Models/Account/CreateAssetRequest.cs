using System.ComponentModel.DataAnnotations;

namespace LMApp.Models.Account
{
    public class CreateAssetRequest
    {
        /// <summary>
        /// Primary type of the asset. Must be one of: cash, credit, investment, real estate, loan, vehicle, cryptocurrency, employee compensation, other liability, other asset
        /// </summary>
        [Required]
        public string type_name { get; set; }

        /// <summary>
        /// Optional asset subtype. Examples include: retirement, checking, savings, prepaid credit card
        /// </summary>
        public string subtype_name { get; set; }

        /// <summary>
        /// Name of the asset
        /// </summary>
        [Required]
        public string name { get; set; }

        /// <summary>
        /// Display name of the asset
        /// </summary>
        public string display_name { get; set; }

        /// <summary>
        /// Current balance of the asset in numeric format to 4 decimal places
        /// </summary>
        public decimal? balance { get; set; }

        /// <summary>
        /// Date/time the balance was last updated in ISO 8601 extended format
        /// </summary>
        public DateTime? balance_as_of { get; set; }

        /// <summary>
        /// Three-letter lowercase currency code of the balance in ISO 4217 format
        /// </summary>
        public string currency { get; set; }

        /// <summary>
        /// Name of institution holding the asset
        /// </summary>
        public string institution_name { get; set; }

        /// <summary>
        /// The date this asset was closed (optional)
        /// </summary>
        public DateTime? closed_on { get; set; }

        /// <summary>
        /// If true, this asset will not show up as an option for assignment when creating transactions manually
        /// </summary>
        public bool? exclude_transactions { get; set; }
    }
}
