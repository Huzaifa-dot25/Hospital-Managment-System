using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.User
{
    public class UpdateUserRoleDto
    {
        [Required]
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
