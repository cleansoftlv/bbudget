using Microsoft.EntityFrameworkCore;
using Services.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DAL
{
    public class CommonContext : DbContext
    {
        public CommonContext(DbContextOptions<CommonContext> options) : base(options)
        {
        }

        public CommonContext()
        {

        }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<UserSso> UserSsos { get; set; }
        public DbSet<LMAccount> LMAccounts { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<PaidLicense> PaidLicenses { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<ProductPrice> ProductPrices { get; set; }

        public DbSet<LMAccountLicense> LMAccountLicenses { get; set; }

    }
}
