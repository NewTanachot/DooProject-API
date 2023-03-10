using DooProject.Datas;
using DooProject.Interfaces;
using DooProject.Models;
using Microsoft.EntityFrameworkCore;

namespace DooProject.Services
{
    public class TransactionServices : ITransactionServices
    {
        private readonly DatabaseContext context;
        private readonly ILogger<TransactionServices> logger;

        public TransactionServices(DatabaseContext context, ILogger<TransactionServices> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<object> GetTransactionAsync(string? productId = null)
        {
            return await context.ProductTransactions
                .Include(x => x.ProductLookUp)
                .ThenInclude(x => x.User)
                .Where(x => !x.ProductLookUp.IsDeleted && (productId == null || x.ProductLookUp.ProductId == productId))
                .Select(x =>
                    new
                    {
                        x.TransactionID,
                        x.ProductLookUp.User.Id,
                        x.ProductLookUp.User.UserName,
                        x.ProductLookUp.ProductId,
                        x.ProductLookUp.ProductName,
                        x.Quantity,
                        x.TransactionDate,
                        x.TransactionType
                    })
                .OrderByDescending(x => x.TransactionDate)
                .ToListAsync();
        }

        public async Task<object> GetUserTransactionAsync(string userId, string? productId = null)
        {
            return await context.ProductTransactions
                .Include(x => x.ProductLookUp)
                .ThenInclude(x => x.User)
                .Where(x => !x.ProductLookUp.IsDeleted &&
                    x.ProductLookUp.User.Id == userId &&
                    (productId == null || x.ProductLookUp.ProductId == productId))
                .Select(x =>
                    new
                    {
                        x.TransactionID,
                        x.ProductLookUp.User.Id,
                        x.ProductLookUp.User.UserName,
                        x.ProductLookUp.ProductId,
                        x.ProductLookUp.ProductName,
                        x.Quantity,
                        x.TransactionDate,
                        x.TransactionType
                    })
                .OrderByDescending(x => x.TransactionDate)
                .ToListAsync();
        }

        public async Task<bool> AddTransactionAsync(ProductLookUp product, int quantity, string type)
        {
            try
            {
                // Check if TransectionAmoung is not 0
                if (quantity != 0)
                {
                    // Add new Transection
                    await context.ProductTransactions.AddAsync(new ProductTransaction
                    {
                        ProductLookUp = product,
                        Quantity = quantity,
                        TransactionType = type
                    });

                    await context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return false;
            }
        }
    }
}
