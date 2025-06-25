using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Configuration
{
    public class LocalOptions
    {
        public string Culture { get; set; }
        public string Enviroment { get; set; }

        public string CdnHost { get; set; }

        public string Version { get; set; }

        public string ApplicationInsightsConnectionString { get; set; }

        public string CdnUrl
        {
            get
            {
                if (String.IsNullOrEmpty(CdnHost))
                    return null;

                return $"https://{CdnHost}/{Version}/";
            }
        }
    }
}
