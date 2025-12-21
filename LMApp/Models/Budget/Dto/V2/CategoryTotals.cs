namespace LMApp.Models.Budget.Dto.V2
{
    public class CategoryTotals
    {
        public decimal other_activity { get; set; }
        
        public decimal recurring_activity { get; set; }
        
        public decimal recurring_remaining { get; set; }
        
        public decimal recurring_expected { get; set; }
        
        public decimal? budgeted { get; set; }
        
        public decimal? available { get; set; }
    }
}
