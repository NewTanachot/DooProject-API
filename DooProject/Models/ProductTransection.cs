using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
