using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Helpers
{
    public static class DateTimeHelper
    {
        public static long ToUnixTimestampSec(this DateTime dateTimeUtc)
        {
            return (long)(dateTimeUtc - UnixEpoch).TotalSeconds;
        }

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
