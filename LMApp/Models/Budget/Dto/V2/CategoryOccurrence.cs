using System.Text.Json.Serialization;

namespace LMApp.Models.Budget.Dto.V2
{
    public class CategoryOccurrence
    {
        public bool current { get; set; }
        
        public string start_date { get; set; }
        
        public string end_date { get; set; }
        
        public decimal other_activity { get; set; }
        
        public decimal recurring_activity { get; set; }
        
        public decimal? budgeted { get; set; }

        /// <summary>
        /// The budgeted amount in the original currency (can be string or null in JSON)
        /// </summary>
        /// 

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal? budgeted_amount { get; set; }
        
        public string budgeted_currency { get; set; }
        
        public string notes { get; set; }
    }
}
