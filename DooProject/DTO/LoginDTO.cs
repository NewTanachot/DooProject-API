using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DooProject.DTO
{
    public class LoginDTO
    {
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [PasswordPropertyText]
        public string Password { get; set; } = string.Empty;
    }
}
