using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DAL.Entities
{
    [PrimaryKey(nameof(provider), nameof(sso_id))]
    [Table(nameof(UserSso))]
    public class UserSso
    {
        public int user_id { get; set; }

        public string provider { get; set; }

        public string sso_id { get; set; }

        public string name { get; set; }
    }
}
