using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LMFunctions.Extensions
{
    public class ExceptionLoggingMiddleware : IFunctionsWorkerMiddleware
    {
        private ILoggerFactory _logFactory;

        public ExceptionLoggingMiddleware(ILoggerFactory logprovider)
        {
            _logFactory = logprovider;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                _logFactory.CreateLogger<ExceptionLoggingMiddleware>()
                    .LogError(ex, "Unhandled function error");
                throw;
            }
        }
    }
}
