using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class DeleteGroupResponse: ResponseWithErrors
    {
        public long[] transactions { get; set; }
    }
}
