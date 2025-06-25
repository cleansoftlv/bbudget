using Microsoft.Extensions.Caching.Memory;
using Services.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Helpers
{
    public class Throttler(LocalCache cache)
    {
        private readonly LocalCache _cache = cache;

        public bool TryRegisterRequest(string name, int requestPerInternal, int intervalSeconds = 60)
        {
            var cacheKey = $"throttle_{name}";
            var now = DateTime.UtcNow;

            var requests = _cache.Cache.Get<List<DateTime>>(cacheKey);
            if (requests != null)
            {
                requests.RemoveAll(r => r < now.AddSeconds(-intervalSeconds));
                if (requests.Count >= requestPerInternal)
                {
                    return false;
                }
            }
            else
            {
                requests = new List<DateTime>();
            }
            requests.Add(now);
            _cache.Cache.Set(cacheKey, requests, now.AddSeconds(intervalSeconds));
            return true;
        }
    }
}
