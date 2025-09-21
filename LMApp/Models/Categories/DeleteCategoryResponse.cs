using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Categories
{
    public class DeleteCategoryResponse
    {

        public CategoryDependents dependents { get; set; }

        public class CategoryDependents
        {
            public string category_name { get; set; }

            public long budget { get; set; }
            public long category_rules { get; set; }
            public long transactions { get; set; }

            public long children { get; set; }

            public long recurring { get; set; }



        }
    }
}
