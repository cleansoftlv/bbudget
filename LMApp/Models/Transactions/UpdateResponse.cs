﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Transactions
{
    public class UpdateResponse: ResponseWithErrors
    {
        public bool updated { get; set; }

        public long[] split { get; set; }
    }
}
