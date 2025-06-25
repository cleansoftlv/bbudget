using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Licensing.Revolut.Dto
{
    public class CreateWebhookRequest
    {
        public string url { get; set; }

        public string[] events { get; set; }
    }
}
