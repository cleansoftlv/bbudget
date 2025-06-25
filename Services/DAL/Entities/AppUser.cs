using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DAL.Entities
{
    [PrimaryKey(nameof(id))]
    [Table(nameof(AppUser))]
    public class AppUser
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string name { get; set; }
        public DateTime created { get; set; }

        public DateTime? deleted { get; set; }

        public DateTime last_activity_date { get; set; }
    }
}
