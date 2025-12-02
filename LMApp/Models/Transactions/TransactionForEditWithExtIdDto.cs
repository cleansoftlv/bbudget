using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class TransactionForEditWithExtIdDto : TransactionForEditDto
    {
        public string external_id { get; set; }
    }
}
