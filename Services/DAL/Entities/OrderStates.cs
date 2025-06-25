using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DAL.Entities
{
    public static class OrderStates
    {
        public const string Pending = "pending";
        public const string Processing = "processing";
        public const string Authorized = "authorised";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";
        public const string Failed = "failed";
    }
}
