using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Helpers
{
    public class TextHelper
    {
        public static string LimitLength(string s, int maxLength, string trimToken = "…")
        {
            if (s == null || s == "" || s.Length <= maxLength || maxLength < trimToken.Length)
                return s;

            if (maxLength == 0)
                return "";

            if (maxLength == trimToken.Length)
                return trimToken;

            bool removeSurrogate = Char.IsHighSurrogate(s[maxLength - trimToken.Length - 1])
                || Char.IsLowSurrogate(s[maxLength - trimToken.Length]);

            return String.Concat(s.Substring(0, maxLength - trimToken.Length - (removeSurrogate ? 1 : 0)), trimToken);
        }
    }
}
