using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Helpers
{
    public static class HttpExtensions
    {
        public static async ValueTask EnsureSuccessStatusCodeIncludeBody(this HttpResponseMessage msg)
        {
            if (!msg.IsSuccessStatusCode)
            {
                var body = await msg.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Request failed with status code {msg.StatusCode}.Response - {body}");
            }
        }

    }
}
