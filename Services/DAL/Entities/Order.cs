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
    [Table(nameof(Order))]
    public class Order
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public int product_id { get; set; }
        public int user_id { get; set; }
        public DateTime created { get; set; }
        public DateTime modified { get; set; }

        [DefaultValue(OrderStatus.New)]
        public OrderStatus status { get; set; } = OrderStatus.New;

        [Precision(18, 2)]
        public decimal amount { get; set; }

        public string currency { get; set; }

        public int product_price_id { get; set; }

        public string external_id { get; set; }

        public string external_status { get; set; }
    }
}
