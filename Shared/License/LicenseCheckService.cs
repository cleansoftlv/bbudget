using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.License
{
    public class LicenseCheckService
    {
        public string SignLowSecurity(
            int userId, 
            string name, 
            long timestamp,
            int nonce, 
            string secret,
            IEnumerable<DateTime> licenseDates)
        {
            var sb = new StringBuilder();
            sb.Append(userId);
            sb.Append("#");
            sb.Append(name);
            sb.Append("#");
            sb.Append(timestamp);
            sb.Append("#");
            sb.Append(nonce);
            sb.Append("#");
            sb.Append(secret);
            foreach (var date in licenseDates.OrderBy(x=>x))
            {
                sb.Append("#");
                sb.Append(date.ToUnixTimestampSec());
            }
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var signBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToBase64String(signBytes);
        }

        public RequestSignature GetNewUnsigned()
        {
            var timestamp = DateTime.UtcNow.ToUnixTimestampSec();
            var nonce = Random.Shared.Next();
            return new RequestSignature
            {
                Timestamp = timestamp,
                Nonce = nonce
            };
        }

        public static string FormatProductInterval(int days)
        {
            if (days == -1)
            {
                return "1 month";
            }
            else if (days == -2)
            {
                return "1 year";
            }
            else if (days == 1)
            {
                return "1 day";
            }
            else if (days >= 0)
            {
                return $"{days} days";
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(days), "Days cannot be negative (except for -1 and -2)");
            }
        }
    }
}
