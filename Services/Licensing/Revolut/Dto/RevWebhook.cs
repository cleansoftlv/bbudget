using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Licensing.Revolut.Dto
{
    public class RevWebhook
    {
        public string id { get; set; }
        public string url { get; set; }
        public string[] events { get; set; }
        public string signing_secret { get; set; }
    }
}
