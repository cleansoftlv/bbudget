using Microsoft.EntityFrameworkCore;
using Services.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DAL.Entities
{

    [PrimaryKey(nameof(id))]
    [Table(nameof(Product))]
    public class Product
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string  name { get; set; }

        public int days { get; set; }

        public bool is_archived { get; set; }
    }
}
