using DooProject.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DooProject.Models
{
    public class ProductLookUp
    {
        [Key]
        [Column(TypeName = "varchar(50)")]
        public string ProductId { get; set; } = Guid.NewGuid().ToString().ToUpper();

        [MaxLength(50)]
        public string ProductName { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; } = new IdentityUser();
        
        public DateTime CreateTime { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;
    }
}
