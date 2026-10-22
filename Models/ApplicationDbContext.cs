using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace nhdphuong_1787_api.Models
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<ChildProfile> ChildProfiles { get; set; }
        public DbSet<AppRule> AppRules { get; set; }
        public DbSet<ScreenTimeSchedule> ScreenTimeSchedules { get; set; }
        public DbSet<AlertLog> AlertLogs { get; set; }
        public DbSet<StaffCatalogItem> StaffCatalogItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ChildProfile parent relation
            builder.Entity<ChildProfile>()
                .HasOne(c => c.Parent)
                .WithMany(u => u.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure AppRule relation
            builder.Entity<AppRule>()
                .HasOne(r => r.ChildProfile)
                .WithMany(c => c.AppRules)
                .HasForeignKey(r => r.ChildProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ScreenTimeSchedule relation
            builder.Entity<ScreenTimeSchedule>()
                .HasOne(s => s.ChildProfile)
                .WithMany(c => c.Schedules)
                .HasForeignKey(s => s.ChildProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure AlertLog relation
            builder.Entity<AlertLog>()
                .HasOne(a => a.ChildProfile)
                .WithMany(c => c.AlertLogs)
                .HasForeignKey(a => a.ChildProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
