using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.License
{
    public class RequestSignature
    {
        public long Timestamp { get; set; }
        public int Nonce { get; set; }
        public string Signature { get; set; }
    }
}
