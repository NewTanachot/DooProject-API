using DooProject.DTO;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DooProject.Models
{
    public class ProductLookUp
    {
        [Key]
        public int ProductId { get; set; }

        [MaxLength(50)]
        public string ProductName { get; set; } = string.Empty;

        public DateTime CreateTime { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        //[JsonIgnore]
        //public List<ProductTransection> ProductTransections { get; set; } = new List<ProductTransection>();
    }
}
