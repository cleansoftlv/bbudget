using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Auth
{
    public enum SsoAddResult
    {
        Ok, Duplicate, Error, TakenFromOtherUser, CannotTakeFromOtherUser
    }
}
