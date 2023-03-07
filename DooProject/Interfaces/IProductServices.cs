using DooProject.DTO;
using DooProject.Models;
using System.Security.Claims;

namespace DooProject.Interfaces
{
    public interface IProductServices
    {
        Task<Response?> AddProductAsync(ProductDTO_Post productDTO, string userId);
        bool CheckNoPermission(string userId, string productUserId);
        Task<Response?> EditProductAsync(ProductDTO_Put productDTO, ProductLookUp Product, string userId);
        Task<ProductLookUp?> FindPoductByIdAsync(string productId, bool includeUser = true);
        Task<object?> GetProductAsync(string? productId = null);
        Task<object?> GetUserProductAsync(string userId, string? productId = null);
    }
}