using Microsoft.EntityFrameworkCore;
using Services.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Services.DAL.Entities
{
    [PrimaryKey(nameof(lm_account_id))]
    [Table(nameof(LMAccountLicense))]

    public class LMAccountLicense
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long lm_account_id { get; set; }
        public DateTime date_from { get; set; }
        public DateTime date_to { get; set; }
        public bool is_paid { get; set; }
        public DateTime created { get; set; }
        public DateTime modified { get; set; }
        public int by_user_id { get; set; }
        public int? paid_license_id { get; set; }
    }
}
