using System.Collections.Generic;
using Auth.Context;
using Microsoft.AspNetCore.Identity;

namespace Auth.Models
{
    public class ChangeRoleViewModel
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public List<AppRole> AllRoles { get; set; }
        public IList<string> UserRoles { get; set; }

        public ChangeRoleViewModel()
        {
            AllRoles = new List<AppRole>();
            UserRoles = new List<string>();
        }
    }
}