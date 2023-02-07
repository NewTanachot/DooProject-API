using DooProject.Models;
using System.Text.Json.Serialization;

namespace DooProject.DTO
{
    public class ProdoctDTO_Get
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public DateTime CreateTime { get; set; }

        public static ProdoctDTO_Get ProdoctDTOMapper(int id, string name, DateTime time)
        {
            return new ProdoctDTO_Get
            {
                ProductId = id,
                ProductName = name,
                CreateTime = time,
            };
        }
    }
}
