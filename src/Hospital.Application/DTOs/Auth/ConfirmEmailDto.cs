using System.ComponentModel.DataAnnotations;

namespace Hospital.Application.DTOs.Auth
{
    public class ConfirmEmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
