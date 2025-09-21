using LMApp.Models.Account;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Categories
{
    public class UpsertCategoryRequest
    {
        [StringLength(40, MinimumLength = 1)]
        public string name { get; set; }

        [StringLength(140)]
        public string description { get; set; }
        public bool is_income { get; set; }
        public bool exclude_from_budget { get; set; }
        public bool exclude_from_totals { get; set; }
        public bool archived { get; set; }
        public long? group_id { get; set; }


        public CategoryItem ToCategoryItem(long id)
        {
            return new CategoryItem
            {
                id = id,
                name = this.name,
                description = this.description,
                is_income = this.is_income,
                exclude_from_budget = this.exclude_from_budget,
                exclude_from_totals = this.exclude_from_totals,
                archived = this.archived,
                group_id = this.group_id
            };
        }
    }
}
