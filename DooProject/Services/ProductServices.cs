using DooProject.Datas;
using DooProject.DTO;
using DooProject.Interfaces;
using DooProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DooProject.Services
{
    public class ProductServices : IProductServices
    {
        private readonly DatabaseContext context;
        private readonly ILogger<ProductServices> productLogger;
        private readonly UserManager<IdentityUser> userManager;

        public ProductServices(DatabaseContext context, ILogger<ProductServices> logger, UserManager<IdentityUser> userManager)
        {
            this.context = context;
            this.productLogger = logger;
            this.userManager = userManager;
        }

        public async Task<object?> GetProductAsync(string? productId = null)
        {
            try
            {
                return await context.ProductLookUps
                    .Where(x => !x.IsDeleted && (productId == null || x.ProductId == productId))
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
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return null;
            }
        }

        public async Task<object?> GetUserProductAsync(string userId, string? productId = null)
        {
            try
            {
                return await context.ProductLookUps
                    .Where(x => !x.IsDeleted && x.User.Id == userId && (productId == null || x.ProductId == productId))
                    .Select(x =>
                        new
                        {
                            x.ProductId,
                            x.ProductName,
                            x.ProductDescription,
                            // Sum all TransectionAmount if it not null or return 0
                            ProductQuantity = x.ProductTransections.Sum(s => s.Quantity),
                            x.MFD,
                            x.EXD,
                            x.ProductAddDate
                        })
                    .OrderBy(x => x.ProductName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return null;
            }
        }

        public async Task<Response?> AddProductAsync(ProductDTO_Post productDTO, string userId)
        {
            try
            {
                // Find UserClaim by userId  and  Check if not exists
                var user = await FindUserAsync(userId);
                if (user == null)
                {
                    throw new Exception("UserId {userId} not found");
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
                    // need to set SetDelete to false
                    isStockExist.IsDeleted = false;

                    // Add ResultValue for Updating old Product
                    ResultValue = $"Add {productDTO.ProductName.Trim()} Success (By restore old product).";
                }

                // If Product was found in general (Bad case scenario)
                else
                {
                    productLogger.LogWarning("Product name is duplicate.");
                    //return BadRequest(new { Error = "Product name is duplicate." });
                }

                // ======================== [ Transection Initialize ] ======================== //

                // If Product have a Initialize number
                if (productDTO.ProductQuantity != 0)
                {
                    // Add new ProductTransections
                    await context.ProductTransections.AddAsync(new ProductTransection
                    {
                        ProductLookUp = NewProduct,
                        Quantity = productDTO.ProductQuantity,
                        TransectionType = "Initialize Number"
                    });
                }

                await context.SaveChangesAsync();
                return new Response
                {
                    IsSuccess = true,
                    Message = ResultValue
                };
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return null;
            }
        }

        public async Task<Response?> EditProductAsync(ProductDTO_Put productDTO, ProductLookUp Product, string userId)
        {
            try
            {
                // Check ProductName is not null
                if (!string.IsNullOrEmpty(productDTO.ProductName))
                {
                    // Check duplicate ProductName 
                    if (await context.ProductLookUps.FirstOrDefaultAsync(x => x.User.Id == userId && x.ProductName == productDTO.ProductName) != null)
                    {
                        return new Response
                        {
                            IsSuccess = false,
                            Message = "Product name is duplicate."
                        };
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
                return new Response
                {
                    IsSuccess = true,
                    Message = $"Edit {Product.ProductName} Success."
                };
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return null;
            }
        }

        public async Task<ProductLookUp?> FindPoductByIdAsync(string productId)
        {
            // Product can be both between obj and null
            return await context.ProductLookUps
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.ProductId == productId);
        }

        // Check if Claim Id is exist 
        public bool CheckIdClaimExist(List<Claim> userClaims, out string userId)
        {
            userId = userClaims.FirstOrDefault(x => x.Type == "Id")?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(userId))
            {
                productLogger.LogWarning("Invalid Token Structure (No UserId).");
                return false;
            }

            return true;
        }

        // Check ProductUserId and UserId from header is the same or not
        public bool CheckNoPermission(string userId, string productUserId)
        {
            return !userId.Equals(productUserId);
        }

        // PRIVATE... Find IdentityUser (User object) method
        private async Task<IdentityUser?> FindUserAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                productLogger.LogWarning($"UserId {userId} not found");
                return null;
            }

            return user;
        }
    }
}
