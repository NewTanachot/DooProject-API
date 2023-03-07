using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DooProject.Models
{
    public class ProductTransaction
    {
        [Key]
        [Column(TypeName = "varchar(50)")]
        public string TransactionID { get; set; } = "T_" + Guid.NewGuid().ToString().ToUpper();

        [MaxLength(50)]
        public string TransactionType { get; set; } = string.Empty;

        // 1:n relationship (one)
        [ForeignKey("ProductId")]
        public ProductLookUp ProductLookUp { get; set; } = new ProductLookUp();

        public int Quantity { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;
    }
}
