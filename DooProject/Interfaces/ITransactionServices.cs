using DooProject.Models;

namespace DooProject.Interfaces
{
    public interface ITransactionServices
    {
        Task<bool> AddTransactionAsync(ProductLookUp product, int quantity, string type);
        Task<object> GetTransactionAsync(string? productId = null);
        Task<object> GetUserTransactionAsync(string userId, string? productId = null);
    }
}