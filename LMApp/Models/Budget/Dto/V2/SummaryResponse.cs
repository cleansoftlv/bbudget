namespace LMApp.Models.Budget.Dto.V2
{
    public class SummaryResponse
    {
        public bool aligned { get; set; }
        
        public List<SummaryCategory> categories { get; set; }
        
        /// <summary>
        /// Total inflow and outflow. Only returned when include_totals=true
        /// </summary>
        public SummaryTotals totals { get; set; }
    }
}
