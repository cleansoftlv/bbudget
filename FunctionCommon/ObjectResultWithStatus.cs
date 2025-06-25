using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FunctionCommon
{
    public class ObjectResultWithStatus: ObjectResult
    {
        public ObjectResultWithStatus(int status, object obj)
            : base(obj)
        {
            StatusCode = status;
        }

        public ObjectResultWithStatus(HttpStatusCode status, object obj)
            : base(obj)
        {
            StatusCode = (int)status;
        }
    }
}
