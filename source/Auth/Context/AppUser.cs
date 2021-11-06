using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Auth.Context
{
    public class AppUser : IdentityUser
    {
        public string RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        [InverseProperty(nameof(AppRole.Users))]
        public virtual AppRole Role { get; set; }
    }

    public class AppRole : IdentityRole
    {
        public AppRole()
        {
            Users = new HashSet<AppUser>();
        }

        public AppRole(string name) : base(name)
        {
            Users = new HashSet<AppUser>();
        }

        [InverseProperty(nameof(AppUser.Role))]
        public virtual ICollection<AppUser> Users { get; set; }
    }
}