namespace DooProject.DTO
{
    public class ProductDTO_Post
    {
        public string ProductName { get; set; } = string.Empty;

        public string? ProductDescription { get; set; }

        public int ProductQuantity { get; set; }

        public DateTime? MFD { get; set; }

        public DateTime? EXD { get; set;}
    }
}
