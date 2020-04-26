using Alias.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alias {
    public class EFContext : DbContext {
        public DbSet<User> Users { get; set; }

        public EFContext(DbContextOptions<EFContext> options)
            : base(options) {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            base.OnConfiguring(optionsBuilder);
        }
    }
}
