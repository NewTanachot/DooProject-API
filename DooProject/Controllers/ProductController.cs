﻿using DooProject.CustomExceptions;
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
                return Ok(await productServices.GetProductAsync());
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
                return Ok(await productServices.GetProductAsync(productId));
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
                // Get UserId in middleware and check if not null
                var userId = HttpContext.Items["UserId"]?.ToString() ?? throw new ArgumentNullException();

                // Find and return all Product 
                return Ok(await productServices.GetUserProductAsync(userId));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"{ex.Message} {Environment.NewLine} {ex.StackTrace}");
            }
        }

        [Authorize]
        [HttpGet("[action]/{productId}")]
        public async Task<IActionResult> GetProductAsync(string productId)
        {
            try
            {
                // Get UserId in middleware and check if not null
                var userId = HttpContext.Items["UserId"]?.ToString() ?? throw new ArgumentNullException();

                // Find and return all Product 
                return Ok(await productServices.GetUserProductAsync(userId, productId));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"{ex.Message} {Environment.NewLine} {ex.StackTrace}");
            }
        }

        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> AddProductAsync([FromBody] ProductDTO_Post productDTO)
        {
            try
            {
                // Get UserId in middleware and check if not null
                var userId = HttpContext.Items["UserId"]?.ToString() ?? throw new ArgumentNullException();

                // Call AddProduct Method Product Services
                var result = await productServices.AddProductAsync(productDTO, userId);

                return Created("Add product done.", null);
            }

            // Check User Notfound Exception from AddProductAsync method
            catch (UserNotFoundException ex)
            {
                productLogger.LogWarning(ex, ex.Message);
                return NotFound(ex.Message);
            }
            // Check duplicate product name  Exception from AddProductAsync method
            catch (DuplicateException ex)
            {
                productLogger.LogWarning(ex, ex.Message);
                return BadRequest(ex.Message);
            }
            // Check some server error
            catch (Exception ex)
            {
                productLogger.LogError(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, $"{ex.Message} {Environment.NewLine} {ex.StackTrace}");
            }
        }

        [Authorize]
        [HttpPut("[action]")]
        public async Task<IActionResult> EditProductAsync([FromBody] ProductDTO_Put productDTO)
        {
            try
            {
                // Get UserId in middleware and check if not null
                var userId = HttpContext.Items["UserId"]?.ToString() ?? throw new ArgumentNullException();

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
                await productServices.EditProductAsync(productDTO, Product, userId);
                return Ok(new { Success = "Edit Product done." });
            }
            // Check duplicate product name  Exception from AddProductAsync method
            catch (DuplicateException ex)
            {
                productLogger.LogWarning(ex, ex.Message);
                return BadRequest(ex.Message);
            }
            // Check some server error
            catch (Exception ex)
            {
                productLogger.LogError(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, $"{ex.Message} {Environment.NewLine} {ex.StackTrace}");
            }
        }

        [Authorize]
        [HttpDelete("[action]/{productId}")]
        public async Task<IActionResult> RemoveProductAsync(string productId)
        {
            try
            {
                // Get UserId in middleware and check if not null
                var userId = HttpContext.Items["UserId"]?.ToString() ?? throw new ArgumentNullException();

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
                // Get UserId in middleware and check if not null
                var userId = HttpContext.Items["UserId"]?.ToString() ?? throw new ArgumentNullException();

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
