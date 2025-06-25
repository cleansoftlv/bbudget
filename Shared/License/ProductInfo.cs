using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.License
{
    public class ProductInfo
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Days { get; set; }

        public List<ProductPriceInfo> Prices { get; set; }
    }
}
