using ClarkAI.Core.Entity.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClarkAI.Infrastructure
{
    public class ClarkContext : DbContext
    {
        public ClarkContext(DbContextOptions<ClarkContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach(var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach(var property in entityType.GetProperties())
                {
                    if(property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            v => v.ToUniversalTime(),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                    }
                }
            }

            modelBuilder.Entity<User>(entity =>
            {

                entity.Property(u => u.Plan)
                .HasConversion<string>();

                entity.Property(u => u.SubscriptionStatus)
                .HasConversion<string>();
            });
            modelBuilder.Entity<Payment>().Property(p => p.Id).ValueGeneratedOnAdd();

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Status)
                .HasConversion<string>();   
            });
        }
    }
}
