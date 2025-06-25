using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DAL.Entities
{
    public enum OrderStatus: byte
    {
        New = 0,
        Created = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4,
        Refunded = 5,
        Failed = 6
    }
}
