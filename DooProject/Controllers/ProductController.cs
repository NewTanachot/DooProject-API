using DooProject.Datas;
using DooProject.DTO;
using DooProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DooProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly DatabaseContext context;
        private readonly UserManager<IdentityUser> userManager;
        private readonly ILogger productLogger;

        public ProductController(DatabaseContext context, UserManager<IdentityUser> userManager, ILoggerFactory logger)
        {
            this.context = context;
            this.userManager = userManager;
            productLogger = logger.CreateLogger<ProductController>();
        }

        [HttpGet("[action]")]
        //[Authorize(Roles = "User")]
        public async Task<IActionResult> GetProductForAllUserAsync()
        {
            try
            {
                return Ok( await context.ProductLookUps
                    .Where(x => !x.IsDeleted)
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
                    .ToListAsync() 
                );
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("[action]/{productId}")]
        public async Task<IActionResult> GetProductForAllUserAsync(string productId)
        {
            try
            {
                return Ok(await context.ProductLookUps
                    .Where(x => !x.IsDeleted && x.ProductId == productId)
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
                    .ToListAsync());
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetProductAsync()
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.FirstOrDefault(x => x.Type == "Id");
                if (userId == null)
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find and return all Product 
                return Ok( await context.ProductLookUps
                    .Where(x => !x.IsDeleted && x.User.Id == userId.Value)
                    .Select(x => 
                        new { 
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
                    .ToListAsync());
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("[action]/{productId}")]
        public async Task<IActionResult> GetProductAsync(string productId)
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.FirstOrDefault(x => x.Type == "Id");
                if (userId == null)
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find and return all Product 
                return Ok(await context.ProductLookUps
                    // Check Delete, Owner and productId
                    .Where(x => !x.IsDeleted && x.User.Id == userId.Value && x.ProductId == productId)
                    .Select(x =>
                        new {
                            x.ProductId,
                            x.ProductName,
                            x.ProductDescription,
                            // Sum all TransectionAmount if it not null or return 0
                            ProductQuantity = x.ProductTransections != null ? x.ProductTransections.Sum(s => s.Quantity) : 0,
                            x.MFD,
                            x.EXD,
                            x.ProductAddDate
                        })
                    .OrderBy(x => x.ProductName)
                    .FirstOrDefaultAsync());
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> AddProductAsync([FromBody] ProductDTO_Post productDTO)
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.FirstOrDefault(x => x.Type == "Id");
                if (userId == null)
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find UserClaim by userId  and  Check if not exists
                var user = await userManager.FindByIdAsync(userId.Value);
                if (user == null)
                {
                    productLogger.LogWarning($"UserId {userId} not found");
                    return NotFound(new { Success = $"UserId {userId} not found" });
                }

                // Find Product by Name
                var isStockExist = await context.ProductLookUps
                    .FirstOrDefaultAsync(x => x.User.Id == userId.Value && x.ProductName == productDTO.ProductName.Trim());

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
                    return BadRequest(new { Error = "Product name is duplicate." });
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
                return Ok(new { Success = ResultValue });
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPut("[action]")]
        public async Task<IActionResult> EditProductAsync([FromBody] ProductDTO_Put prodoctDTO)
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.FirstOrDefault(x => x.Type == "Id");
                if (userId == null)
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find Product by ProductId
                var Product = await context.ProductLookUps
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.ProductId == prodoctDTO.ProductId);

                // Logger for Check Product have Include User or not
                productLogger.LogWarning(JsonConvert.SerializeObject(Product));

                // Check if Product not exist 
                if (Product == null)
                {
                    productLogger.LogWarning("Product Not Found.");
                    return NotFound(new { Error = "Product Not Found." });
                }

                // Check Product owner by UserId
                if (!Product.User.Id.Equals(userId.Value))
                {
                    productLogger.LogWarning($"This User doesn't have Permission to access another user data. " +
                        $"ProductUser : {Product.User.Id} and Token : {userId.Value}");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "This User doesn't have Permission to access another user data." });
                }

                // ------- Check Update Section -------

                // Check ProductName is not null
                if (!string.IsNullOrEmpty(prodoctDTO.ProductName))
                {
                    // Check duplicate ProductName 
                    if (await context.ProductLookUps.FirstOrDefaultAsync(x => x.User.Id == userId.Value && x.ProductName == prodoctDTO.ProductName) != null)
                    {
                        productLogger.LogWarning("Product name is duplicate.");
                        return BadRequest(new { Error = "Product name is duplicate." });
                    }

                    Product.ProductName = prodoctDTO.ProductName;
                }

                // Check Product Description is not null
                if (prodoctDTO.ProductDescription != null)
                {
                    Product.ProductDescription = prodoctDTO.ProductDescription;
                }

                // Check Product MFD is not null
                if (prodoctDTO.MFD != null)
                {
                    Product.MFD = prodoctDTO.MFD;
                }

                // Check Product EXD is not null
                if (prodoctDTO.EXD != null)
                {
                    Product.EXD = prodoctDTO.EXD;
                }

                await context.SaveChangesAsync();
                return Ok(new { Success = $"Edit {Product.ProductName} Success." });
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("[action]/{productId}")]
        public async Task<IActionResult> RemoveProductAsync(string productId)
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.FirstOrDefault(x => x.Type == "Id");

                if (userId == null)
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        Error = "Invalid Token Structure (No UserId)."
                    });
                }

                // Find Product by ProductId for Delete
                var SoftDeleteProduct = await context.ProductLookUps.Include(x => x.User).FirstOrDefaultAsync(x => x.ProductId == productId.Trim());

                // Check Product exists
                if (SoftDeleteProduct == null)
                {
                    productLogger.LogWarning("Product not found.");
                    return NotFound(new { Error = "Product not found" });
                }

                // Check Product owner by UserId
                if (!SoftDeleteProduct.User.Id.Equals(userId.Value))
                {
                    productLogger.LogWarning("This User doesn't have Permission to access another user data.");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "This User doesn't have Permission to access another user data." });
                }

                // Set Delete to Product 
                SoftDeleteProduct.IsDeleted = true;

                await context.SaveChangesAsync();
                return Ok(new { Success = $"Delete Product {SoftDeleteProduct.ProductId} Done." });
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPut("[action]/{productId}")]
        public async Task<IActionResult> RestoreProductAsync(string productId)
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.FirstOrDefault(x => x.Type == "Id");

                if (userId == null)
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        Error = "Invalid Token Structure (No UserId)."
                    });
                }

                // Find Product by productId
                var DeletedProduct = await context.ProductLookUps.FirstOrDefaultAsync(x => x.ProductId == productId);

                // Check if Delete Product exists
                if (DeletedProduct == null)
                {
                    productLogger.LogWarning("Product not found.");
                    return NotFound(new { Error = "Product not found" });
                }

                // Restore Deleted Product
                DeletedProduct.IsDeleted = false;

                await context.SaveChangesAsync();
                return Ok(new { Success = $"Restore Product {DeletedProduct.ProductId} Done." });
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
