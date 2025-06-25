using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DAL.Entities
{
    [PrimaryKey(nameof(id))]
    [Table(nameof(PaidLicense))]
    public class PaidLicense
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public int user_id { get; set; }
        public DateTime created { get; set; }
        public DateTime date_from { get; set; }
        public DateTime date_to { get; set; }
        public int order_id { get; set; }
        public bool is_cancelled { get; set; }
    }
}
