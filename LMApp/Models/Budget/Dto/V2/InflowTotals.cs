namespace LMApp.Models.Budget.Dto.V2
{
    public class InflowTotals
    {
        public decimal other_activity { get; set; }
        
        public decimal recurring_activity { get; set; }
        
        public decimal recurring_remaining { get; set; }
        
        public decimal uncategorized { get; set; }
        
        public int uncategorized_count { get; set; }
        
        public decimal uncategorized_recurring { get; set; }
    }
}
