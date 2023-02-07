using DooProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace DooProject.Datas
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<ProductLookUp> ProductLookUps { get; set; }

        public DbSet<ProductTransection> ProductTransections { get; set; }
    }
}
