using DooProject.Datas;
using DooProject.DTO;
using DooProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> GetProductAsync()
        {
            try
            {
                return Ok( await context.ProductLookUps
                    .Where(x => !x.IsDeleted)
                    .Select(x => new { x.ProductId, x.ProductName, UserId = x.User.Id, x.User.UserName, x.CreateTime })
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
        public async Task<IActionResult> GetProductAsync(string productId)
        {
            try
            {
                return Ok(await context.ProductLookUps
                    .Where(x => !x.IsDeleted && x.ProductId.Equals(productId))
                    .Select(x => new { x.ProductId, x.ProductName, UserId = x.User.Id, x.User.UserName, x.CreateTime })
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
        public async Task<IActionResult> GetProductByUserAsync()
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.Where(x => x.Type.Equals("Id")).FirstOrDefault();
                if (userId == null)
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return BadRequest(new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find and return all Product 
                return Ok( await context.ProductLookUps
                    .Where(x => !x.IsDeleted && x.User.Id.Equals(userId.Value))
                    .Select(x => new { x.ProductId, x.ProductName, x.CreateTime })
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
        public async Task<IActionResult> GetProductByUserAsync(string productId)
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.Where(x => x.Type.Equals("Id")).FirstOrDefault();
                if (userId == null)
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return BadRequest(new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find and return all Product 
                return Ok(await context.ProductLookUps
                    // Check Delete, Owner and productId
                    .Where(x => !x.IsDeleted && x.User.Id.Equals(userId.Value) && x.ProductId.Equals(productId))
                    .Select(x => new { x.ProductId, x.ProductName, x.CreateTime })
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
        public async Task<IActionResult> AddProductByUserAsync([FromBody] ProductDTO_Post productDTO)
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.Where(x => x.Type.Equals("Id")).FirstOrDefault();
                if (userId == null)
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return BadRequest(new { Error = "Invalid Token Structure (No UserId)." });
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
                    .Where(x => x.ProductName.Equals(productDTO.ProductName.Trim()))
                    .FirstOrDefaultAsync();

                // Check if Product found on Deleted Product
                if (isStockExist != null && isStockExist.IsDeleted)
                {
                    // Delete the old Product in Trash ( Also delete transection data with foreign key )
                    context.ProductLookUps.Remove(isStockExist);
                }

                // If Product was found on general (Bad case for addProduct Method)
                else
                {
                    productLogger.LogWarning("Product name is duplicate.");
                    return BadRequest(new { Error = "Product name is duplicate." });
                }

                // Create new Product for add (and also add User)
                var NewProduct = new ProductLookUp
                {
                    ProductName = productDTO.ProductName.Trim(),
                    User = user
                };

                // Add New Product
                await context.ProductLookUps.AddAsync(NewProduct);

                // If Product have a Initialize number
                if (productDTO.ProductAmoungInit != 0)
                {
                    // Add new ProductTransections
                    await context.ProductTransections.AddAsync(new ProductTransection
                    {
                        ProductLookUp = NewProduct,
                        TransectionAmount = productDTO.ProductAmoungInit,
                        TransectionDescription = "Initialize Number"
                    });
                }

                await context.SaveChangesAsync();
                return Ok(new { Success = $"Add {productDTO.ProductName} Success." });
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPut("[action]")]
        public async Task<IActionResult> EditProductByUserAsync([FromBody] ProductDTO_Put prodoctDTO)
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.Where(x => x.Type == "Id").FirstOrDefault();

                if (userId != null)
                {
                    // Find Product by ProductId
                    var Product = await context.ProductLookUps
                        .Include(x => x.User)
                        .FirstOrDefaultAsync(x => x.ProductId.Equals(prodoctDTO.ProductId));

                    // Product exists
                    if (Product != null)
                    {
                        // Check Product owner by UserId
                        if (Product.User.Id.Equals(userId.Value))
                        {
                            // Update ProductName
                            Product.ProductName = prodoctDTO.ProductName;

                            await context.SaveChangesAsync();
                            return Ok(new { Success = $"Edit {Product.ProductName} Success." });
                        }

                        productLogger.LogWarning("This User doesn't have Permission to access another user data.");
                        return NotFound(new { Error = "This User doesn't have Permission to access another user data." });
                    }

                    productLogger.LogWarning("Product Not Found.");
                    return NotFound(new { Error = "Product Not Found." });
                }

                productLogger.LogWarning("Invalid Token Structure (No UserId).");
                return BadRequest(new { Error = "Invalid Token Structure (No UserId)." });
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("[action]")]
        public async Task<IActionResult> RemoveProductByUserAsync(string productId)
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.Where(x => x.Type == "Id").FirstOrDefault();

                if (userId == null)
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return BadRequest(new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find Product by ProductId for Delete
                var DeleteProduct = await context.ProductLookUps.Include(x => x.User).FirstOrDefaultAsync(x => x.ProductId.Equals(productId.Trim()));

                // Check Product exists
                if (DeleteProduct == null)
                {
                    productLogger.LogWarning("Product not found.");
                    return NotFound(new { Error = "Product not found" });
                }

                // Check Product owner by UserId
                if (!DeleteProduct.User.Id.Equals(userId.Value))
                {
                    productLogger.LogWarning("This User doesn't have Permission to access another user data.");
                    return NotFound(new { Error = "This User doesn't have Permission to access another user data." });
                }

                // Set Delete to Product 
                DeleteProduct.IsDeleted = true;

                await context.SaveChangesAsync();
                return Ok(new { Success = $"Delete Product {DeleteProduct.ProductName} Done." });
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
