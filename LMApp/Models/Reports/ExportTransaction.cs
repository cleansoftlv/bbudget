using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Reports
{
    public class ExportTransaction
    {
        public long Id { get; set; }
        public string Date { get; set; }
        public string Payee { get; set; }

        public string Notes { get; set; }

        public decimal Amount { get; set; }
        public decimal? AmountTo     { get; set; }

        public decimal? TransferAmount { get; set; }
        public string TransferCurrency { get; set; }

        public string Currency { get; set; }
        public string CurrencyTo { get; set; }

        public string Account { get; set; }
        public string AccountTo { get; set; }

        public string Category { get; set; }

        public string Type { get; set; }

        public bool Cleared { get; set; }

        public string Url { get; set; }
    }
}
