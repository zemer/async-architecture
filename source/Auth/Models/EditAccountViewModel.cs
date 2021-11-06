using Auth.Context;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Auth.Models
{
    public class EditAccountViewModel
    {
        public AppUser Account { get; set; }

        public string Role { get; set; }

        public SelectListItem[] AllRoles { get; set; }
    }
}