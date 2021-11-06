using Auth.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Auth.Context
{
    public class DataContext : IdentityDbContext<AppUser>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        #region Overrides of IdentityDbContext<AppUser,AppRole,string,IdentityUserClaim<string>,IdentityUserRole<string>,IdentityUserLogin<string>,IdentityRoleClaim<string>,IdentityUserToken<string>>

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AppRole>()
                .HasData(
                    new AppRole { Name = "Worker", NormalizedName = "WORKER" },
                    new AppRole { Name = "Manager", NormalizedName = "MANAGER" },
                    new AppRole { Name = "Administrator", NormalizedName = "ADMINISTRATOR" });
        }

        #endregion
    }
}