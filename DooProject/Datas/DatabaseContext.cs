using DooProject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace DooProject.Datas
{
    public class DatabaseContext : IdentityDbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<ProductLookUp> ProductLookUps { get; set; }

        public DbSet<ProductTransection> ProductTransections { get; set; }
    }
}
