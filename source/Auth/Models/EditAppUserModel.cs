using System.Collections.Generic;
using Auth.Context;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Auth.Models
{
    public class EditAppUserModel : PageModel
    {
        public AppRole Role { get; set; }

        public IEnumerable<AppRole> Roles { get; set; }
    }
}