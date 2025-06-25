using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DAL
{
    public class LocalCache
    {
        private readonly IMemoryCache _cache;
        public LocalCache()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public IMemoryCache Cache => _cache;
    }
}
