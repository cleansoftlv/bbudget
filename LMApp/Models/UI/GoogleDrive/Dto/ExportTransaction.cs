using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.UI.GoogleDrive.Dto
{
    public class ExportTransaction
    {
        public string Date { get; set; }
        public string Payee { get; set; }

        public decimal Amount { get; set; }

        public decimal? TransferAmount { get; set; }

        public string Currency { get; set; }

        public string Account { get; set; }

        public string Category { get; set; }

        public string Type { get; set; }

        public bool Cleared { get; set; }
    }
}
