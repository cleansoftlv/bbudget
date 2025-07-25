using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Context
{
    public class LocalSettings
    {
        public bool SkipCreateTransferHelp { get; set; }

        public bool ShowAmountSpentInBudget { get; set; }
        public bool ShowMultipleCurrenciesInBudget { get; set; }

        public static LocalSettings CreateDefault() =>
            new LocalSettings
            {
            };
    }
}
