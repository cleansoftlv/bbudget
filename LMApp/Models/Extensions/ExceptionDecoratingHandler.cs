using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Extensions
{
    public class ExceptionDecoratingHandler : DelegatingHandler
    {
        private readonly string _clientName;
        public ExceptionDecoratingHandler(string clientName)
        {
            _clientName = clientName;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                ex.Data[ClientConstants.ClientNameKey] = _clientName;
                throw;
            }
        }
    }
}
