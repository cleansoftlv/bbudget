using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Categories
{
    public class BudgetCategoryConfig
    {
        public long config_id { get; set; }
        public string cadence { get; set; } // e.g. "monthly", "twice a month", etc.
        public decimal amount { get; set; }
        public string currency { get; set; } // ISO 4217 code
        public decimal to_base { get; set; }
    }
}
