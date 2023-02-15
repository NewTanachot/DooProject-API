namespace DooProject.DTO
{
    public class ProductDTO_Put
    {
        public string ProductId { get; set; } = string.Empty;

        public string? ProductName { get; set; }

        public string? ProductDescription { get; set; }

        public DateTime? MFD { get; set; }

        public DateTime? EXD { get; set; }
    }
}
