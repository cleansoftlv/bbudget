using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class ResponseV2WithErrors
    {
        public string message { get; set; }
        public LMApiErrorV2[] errors { get; set; }
    }
}
