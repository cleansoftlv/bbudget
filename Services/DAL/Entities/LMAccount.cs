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
    [Table(nameof(LMAccount))]
    public class LMAccount
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public int user_id { get; set; }
        public string name { get; set; }
        public string token { get; set; }
        public string user_name { get; set; }
        public string additional_currencies { get; set; }
        public bool is_active { get; set; }
        public long? transfer_category_id { get; set; }
        public long? cross_currency_transfer_category_id { get; set; }
        public bool sort_tran_on_load_more { get; set; }

        public long lm_account_id { get; set; }

    }
}
