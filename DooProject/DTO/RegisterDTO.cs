using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DooProject.DTO
{
    public class RegisterDTO
    {
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [PasswordPropertyText]
        public string Password { get; set; } = "Welcome1";

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;
    }
}
