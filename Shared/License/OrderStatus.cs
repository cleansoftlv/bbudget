using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.License
{
    public enum OrderStatus
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
