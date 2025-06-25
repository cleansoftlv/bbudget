using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Options
{
    public class RevolutSettings
    {
        public string ApiVersion { get; set; }

        public string RedirectUrlPrefix { get; set; }
        public string StatementSufix { get; set; }
        public string SettlementCurrency { get; set; }
        public string SecretKey { get; set; }
        public string PublicKey { get; set; }
        public string BaseUrl { get; set; }
        public string WebhookSecret { get; set; }
        public string WebhookSecretTest { get; set; }

        public string WebhookQueueStorageConnectionString { get; set; }

        public string WebhookQueueName { get; set; }
        public string WebhookQueueNameTest { get; set; }
    }
}
