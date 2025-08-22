using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Reports
{
    public class ExportTransactionsCsvRequest
    {
        public string OauthClientId { get; set; }

        public string FileName { get; set; }

        public ExportTransaction[] Transactions { get; set; }
    }
}
