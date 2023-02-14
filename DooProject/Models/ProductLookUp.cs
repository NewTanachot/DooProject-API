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
        public string ProductId { get; set; } = "P_" + Guid.NewGuid().ToString().ToUpper();

        [MaxLength(50)]
        public string ProductName { get; set; } = string.Empty;

        public string? ProductDescription { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; } = new IdentityUser();

        // 1:n relationship (many)
        //[JsonIgnore]
        //[NotMapped]
        public ICollection<ProductTransection> ProductTransections { get; set; } = new List<ProductTransection>();

        public DateTime? MFD { get; set; }

        public DateTime? EXD { get; set; }

        public DateTime ProductAddDate { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;
    }
}
