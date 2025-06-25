using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class CreateGroupDto
    {
        public string date { get; set; }
        public string payee { get; set; }
        public long? category_id { get; set; }
        public string notes { get; set; }
        public List<string> tags { get; set; }
        public long[] transactions { get; set; }

    }
}
