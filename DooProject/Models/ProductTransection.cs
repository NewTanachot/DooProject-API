using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DooProject.Models
{
    public class ProductTransection
    {
        [Key]
        [Column(TypeName = "varchar(50)")]
        public string TransectionID { get; set; } = Guid.NewGuid().ToString().ToUpper();

        public int TransectionAmount { get; set; }

        [MaxLength(100)]
        public string? TransectionDescription { get; set; }

        [ForeignKey("ProductID")]
        public ProductLookUp ProductLookUp { get; set; } = new ProductLookUp();
    }
}
