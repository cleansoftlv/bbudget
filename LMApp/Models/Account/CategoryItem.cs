using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Account
{
    public class CategoryItem
    {

        public long id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public bool is_income { get; set; }
        public bool exclude_from_budget { get; set; }
        public bool exclude_from_totals { get; set; }

        public bool is_group { get; set; }
        public long? group_id { get; set; }
        public long? order { get; set; }

        public bool? archived { get; set; }

        public string group_category_name { get; set; }

        public DateTime? archived_on { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? created_at { get; set; }
        public List<CategoryItem> children { get; set; }

    }
}
