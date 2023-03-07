namespace DooProject.DTO
{
    public class TransactionDTO
    {
        public string ProductID { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string TransactionType { get; set; } = string.Empty;
    }
}
