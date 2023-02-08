using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DooProject.Models
{
    public class ProductTransection
    {
        [Key]
        public int TransectionID { get; set; }

        public int TransectionAmount { get; set; }

        [ForeignKey("ProductID")]
        public ProductLookUp ProductLookUp { get; set; } = new ProductLookUp();
    }
}
