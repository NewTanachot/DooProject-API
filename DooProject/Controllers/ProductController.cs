using DooProject.Datas;
using DooProject.DTO;
using DooProject.Interfaces;
using DooProject.Models;
using DooProject.Services;
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
        private readonly IProductServices productServices;
        private readonly IAuthServices authServices;
        private readonly ILogger productLogger;

        public ProductController(DatabaseContext context, IProductServices productServices, IAuthServices authServices, ILogger<ProductController> logger)
        {
            this.context = context;
            this.productServices = productServices;
            this.authServices = authServices;
            productLogger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetProductForAllUserAsync()
        {
            try
            {
                var result = await productServices.GetProductAsync();

                if (result is string)
                {
                    throw new Exception(result.ToString());
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"{ex.Message} {Environment.NewLine} {ex.StackTrace}" );
            }
        }

        [HttpGet("[action]/{productId}")]
        public async Task<IActionResult> GetProductForAllUserAsync(string productId)
        {
            try
            {
                var result = await productServices.GetProductAsync(productId);

                if (result is string)
                {
                    throw new Exception(result.ToString());
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"{ex.Message} {Environment.NewLine} {ex.StackTrace}");
            }
        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetProductAsync()
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                if (!authServices.CheckIdClaimExist(User.Claims.ToList(), out string userId))
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find and return all Product 
                var result = await productServices.GetUserProductAsync(userId);

                if (result is string)
                {
                    throw new Exception(result.ToString());
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("[action]/{productId}")]
        public async Task<IActionResult> GetProductAsync(string productId)
        {
            try
            {
                // Find userId in Http header  and  Check if it have Id Claim
                if (!authServices.CheckIdClaimExist(User.Claims.ToList(), out string userId))
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find and return all Product 
                var result = await productServices.GetUserProductAsync(userId, productId);

                if (result is string)
                {
                    throw new Exception(result.ToString());
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> AddProductAsync([FromBody] ProductDTO_Post productDTO)
        {
            try
            {
                // Find userId in Http header  and  Check if it have Id Claim
                if (!authServices.CheckIdClaimExist(User.Claims.ToList(), out string userId))
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Call AddProduct Method Product Services
                var result = await productServices.AddProductAsync(productDTO, userId);

                // Check some 500 error
                if (result == null)
                {
                    throw new Exception();
                }

                // Check if duplicate name
                else if (!result.IsSuccess && result.Message.Contains("duplicate"))
                {
                    return BadRequest(result.Message);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPut("[action]")]
        public async Task<IActionResult> EditProductAsync([FromBody] ProductDTO_Put productDTO)
        {
            try
            {
                // Find userId in Http header  and  Check if it have Id Claim
                if (!authServices.CheckIdClaimExist(User.Claims.ToList(), out string userId))
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find Product include User by ProductId
                var Product = await productServices.FindPoductByIdAsync(productDTO.ProductId);

                // Check if Product not exist 
                if (Product == null)
                {
                    productLogger.LogWarning("Product Not Found.");
                    return NotFound(new { Error = "Product Not Found." });
                }

                // Check Product owner by UserId
                if (!productServices.CheckNoPermission(Product.User.Id, userId))
                {
                    productLogger.LogWarning($"This User doesn't have Permission to access another user data.");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "This User doesn't have Permission to access another user data." });
                }

                // ------- Check Update Section -------

                // Call EditProduct Method
                var result = await productServices.EditProductAsync(productDTO, Product, userId);

                // Check some 500 error
                if (result == null)
                {
                    throw new Exception();
                }

                // Check if response is false cause of duplicate Name
                else if (result.IsSuccess && result.Message.Contains("duplicate"))
                {
                    productLogger.LogWarning("Product name is duplicate.");
                    return BadRequest("Product name is duplicate.");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("[action]/{productId}")]
        public async Task<IActionResult> RemoveProductAsync(string productId)
        {
            try
            {
                // Find userId in Http header  and  Check if it have Id Claim
                if (!authServices.CheckIdClaimExist(User.Claims.ToList(), out string userId))
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find Product by ProductId for Delete
                var SoftDeleteProduct = await productServices.FindPoductByIdAsync(productId);

                // Check Product exists
                if (SoftDeleteProduct == null)
                {
                    productLogger.LogWarning("Product not found.");
                    return NotFound(new { Error = "Product not found" });
                }

                // Check Product owner by UserId
                if (productServices.CheckNoPermission(SoftDeleteProduct.User.Id, userId))
                {
                    productLogger.LogWarning("This User doesn't have Permission to access another user data.");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "This User doesn't have Permission to access another user data." });
                }

                // Set Delete to Product 
                SoftDeleteProduct.IsDeleted = true;

                await context.SaveChangesAsync();
                return Ok(new { IsSuccess = $"Delete Product {SoftDeleteProduct.ProductId} Done." });
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
                // Find userId in Http header  and  Check if it have Id Claim
                if (!authServices.CheckIdClaimExist(User.Claims.ToList(), out string userId))
                {
                    productLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find Delete Product by productId
                var DeletedProduct = await productServices.FindPoductByIdAsync(productId);

                // Check if Delete Product not exists
                if (DeletedProduct == null)
                {
                    productLogger.LogWarning("Product not found.");
                    return NotFound(new { Error = "Product not found" });
                }

                // Check Product owner by UserId
                if (productServices.CheckNoPermission(DeletedProduct.User.Id, userId))
                {
                    productLogger.LogWarning("This User doesn't have Permission to access another user data.");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "This User doesn't have Permission to access another user data." });
                }

                // Restore Deleted Product
                DeletedProduct.IsDeleted = false;

                await context.SaveChangesAsync();
                return Ok(new { IsSuccess = $"Restore Product {DeletedProduct.ProductId} Done." });
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
