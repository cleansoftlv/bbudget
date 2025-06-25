using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DAL.Entities
{
    [PrimaryKey(nameof(id))]
    [Table(nameof(ProductPrice))]
    public class ProductPrice
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public int product_id { get; set; }
        [Precision(18, 2)]

        public decimal price { get; set; }
        public string currency { get; set; }
        public bool is_archived { get; set; }
    }
}
