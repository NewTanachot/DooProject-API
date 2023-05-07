using DooProject.CustomExceptions;
using DooProject.Datas;
using DooProject.DTO;
using DooProject.Interfaces;
using DooProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DooProject.Services
{
    public class ProductServices : IProductServices
    {
        private readonly DatabaseContext context;
        private readonly ILogger<ProductServices> productLogger;
        private readonly ITransactionServices transactionServices;
        private readonly IAuthServices authServices;
        private readonly IMemoryCache memoryCache;

        public ProductServices(
            DatabaseContext context, 
            ILogger<ProductServices> logger, 
            ITransactionServices transactionServices, 
            IAuthServices authServices,
            IMemoryCache memoryCache)
        {
            this.context = context;
            this.productLogger = logger;
            this.transactionServices = transactionServices;
            this.authServices = authServices;
            this.memoryCache = memoryCache;
        }

        public async Task<object> GetProductAsync(string? productId = null)
        {
            // declare product object
            object? product;

            // Check if need specific productId
            if (productId != null)
            {
                product = await context.ProductLookUps
                    .Where(x => !x.IsDeleted && x.ProductId == productId)
                    .Take(100)
                    .OrderBy(x => x.ProductAddDate).ThenBy(x => x.ProductName)
                    .Select(x => new
                    {
                        x.ProductId,
                        x.ProductName,
                        x.ProductDescription,
                        x.MFD,
                        x.EXD,
                        UserId = x.User.Id,
                        x.User.UserName,
                        x.ProductAddDate
                    })
                    .ToListAsync();
            }
            // Need all product *Take only 100
            else
            {
                // Get product in MemoryCache by key
                product = memoryCache.Get("ProductItem");

                // Check if cache miss
                if (product == null)
                {
                    // Get new product object from db
                    product = await context.ProductLookUps
                        .Where(x => !x.IsDeleted)
                        .Take(100)
                        .OrderBy(x => x.ProductAddDate).ThenBy(x => x.ProductName)
                        .Select(x => new
                        {
                            x.ProductId,
                            x.ProductName,
                            x.ProductDescription,
                            x.MFD,
                            x.EXD,
                            UserId = x.User.Id,
                            x.User.UserName,
                            x.ProductAddDate
                        })
                        .ToListAsync();

                    // Set cache by MemoryCacheEntryOptions
                    memoryCache.Set("ProductItem", product, new MemoryCacheEntryOptions
                    {
                        Priority = CacheItemPriority.Normal,
                        SlidingExpiration = TimeSpan.FromMinutes(1),
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                }
            }

            return product;
        }

        public async Task<object> GetUserProductAsync(string userId, string? productId = null)
        {
            return await context.ProductLookUps
                .Where(x => !x.IsDeleted && x.User.Id == userId && (productId == null || x.ProductId == productId))
                .Take(100)
                .OrderBy(x => x.ProductAddDate).ThenBy(x => x.ProductName)
                .Select(x =>
                    new
                    {
                        x.ProductId,
                        x.ProductName,
                        x.ProductDescription,
                        // Sum all TransectionAmount if it not null or return 0
                        ProductQuantity = x.ProductTransactions.Sum(s => s.Quantity),
                        x.MFD,
                        x.EXD,
                        x.ProductAddDate
                    })
                .ToListAsync();
        }

        public async Task<bool> AddProductAsync(ProductDTO_Post productDTO, string userId)
        {
            // Find UserClaim by userId  and  Check if not exists
            var user = await authServices.FindUserAsync(userId);
            if (user == null)
            {
                throw new UserNotFoundException($"UserId {userId} not found");
            }

            // Find Product by Name
            var isStockExist = await context.ProductLookUps
                .FirstOrDefaultAsync(x => x.User.Id == userId && x.ProductName == productDTO.ProductName.Trim());

            // Create NewProduct object and Result value
            var NewProduct = new ProductLookUp();
            var ResultValue = string.Empty;

            // Check if Product was not found
            if (isStockExist == null)
            {
                // Create new Product for add (and also add User)
                NewProduct = new ProductLookUp
                {
                    ProductName = productDTO.ProductName.Trim(),
                    ProductDescription = productDTO.ProductDescription,
                    MFD = productDTO.MFD,
                    EXD = productDTO.EXD,
                    User = user
                };

                // Add New Product
                await context.ProductLookUps.AddAsync(NewProduct);

                // Add ResultValue for New Product Creation
                ResultValue = $"Add {productDTO.ProductName.Trim()} Success.";
            }

            // If Product was found in Deleted Product
            else if (isStockExist.IsDeleted)
            {
                isStockExist.ProductName = productDTO.ProductName.Trim();
                isStockExist.ProductDescription = productDTO.ProductDescription;
                isStockExist.MFD = productDTO.MFD;
                isStockExist.EXD = productDTO.EXD;
                isStockExist.IsDeleted = false;

                // Add ResultValue for Updating old Product
                ResultValue = $"Add {productDTO.ProductName.Trim()} Success (By restore old product).";
            }

            // If Product was found in general (Bad case scenario)
            else
            {
                throw new DuplicateException("Product name is duplicate.");
            }

            // ======================== [ Transection Initialize ] ======================== //

            // Add new ProductTransections
            await transactionServices.AddTransactionAsync(NewProduct, productDTO.ProductQuantity, "Initialize quantity");

            await context.SaveChangesAsync();
            // return true If success
            return true;
        }

        public async Task<bool> EditProductAsync(ProductDTO_Put productDTO, ProductLookUp Product, string userId)
        {
            // Check ProductName is not null
            if (!string.IsNullOrEmpty(productDTO.ProductName))
            {
                // Check duplicate ProductName 
                if (await context.ProductLookUps.FirstOrDefaultAsync(x => x.User.Id == userId && x.ProductName == productDTO.ProductName) != null)
                {
                    throw new DuplicateException("Product name is duplicate.");
                }

                Product.ProductName = productDTO.ProductName;
            }

            // Check Product Description is not null
            if (productDTO.ProductDescription != null)
            {
                Product.ProductDescription = productDTO.ProductDescription;
            }

            // Check Product MFD is not null
            if (productDTO.MFD != null)
            {
                Product.MFD = productDTO.MFD;
            }

            // Check Product EXD is not null
            if (productDTO.EXD != null)
            {
                Product.EXD = productDTO.EXD;
            }

            await context.SaveChangesAsync();
            // return true if Edit success
            return true;
        }

        public async Task<ProductLookUp?> FindPoductByIdAsync(string productId, bool includeUser = true)
        {

            var result = new ProductLookUp();

            if (includeUser)
            {
                result = await context.ProductLookUps
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.ProductId == productId);
            }
            else
            {
                result = await context.ProductLookUps.FirstOrDefaultAsync(x => x.ProductId == productId);
            }

            // Product result can be both between obj and null
            return result;
        }

        // Check ProductUserId and UserId from header is the same or not
        public bool CheckNoPermission(string userId, string productUserId)
        {
            return !userId.Equals(productUserId);
        }
    }
}
