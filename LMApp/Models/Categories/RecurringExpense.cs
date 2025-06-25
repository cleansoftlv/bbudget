using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Categories
{
    public class RecurringExpense
    {
        public string payee { get; set; }

        public decimal amount { get; set; }

        public string currency { get; set; }

        public decimal to_base { get; set; }
    }
}
