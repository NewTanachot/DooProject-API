﻿using DooProject.Datas;
using DooProject.DTO;
using DooProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DooProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly DatabaseContext context;
        private readonly ILogger productLogger;

        public ProductController(DatabaseContext context, ILoggerFactory logger)
        {
            this.context = context;
            productLogger = logger.CreateLogger<ProductController>();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetProduct()
        {
            try
            {
                return Ok( await context.ProductLookUps
                    .Where(x => !x.IsDeleted)
                    .Select(x => new { x.ProductId, x.ProductName, x.CreateTime })
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
        public async Task<IActionResult> GetProduct(int productId)
        {
            try
            {
                var Product = await context.ProductLookUps
                    .Where(x => !x.IsDeleted && x.ProductId.Equals(productId))
                    .Select(x => new { x.ProductId, x.ProductName, x.CreateTime })
                    .ToListAsync();

                if (Product.Any())
                {
                    return Ok(Product);
                }

                productLogger.LogWarning("Product Not Found.");
                return NotFound(new { Error = "Product Not Found." });
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AddProduct([FromBody] string productName)
        {
            try
            {
                var ReturnText = string.Empty;
                var isStockExist = await context.ProductLookUps
                    .Where(x => x.ProductName.Equals(productName.Trim()))
                    .FirstOrDefaultAsync();

                if (isStockExist == null)
                {
                    await context.ProductLookUps.AddAsync(new ProductLookUp
                    {
                        ProductName = productName.Trim()
                    });

                    ReturnText = $"Add {productName} Success.";
                }
                else if (isStockExist.IsDeleted)
                {
                    isStockExist.ProductName = productName.Trim();
                    isStockExist.CreateTime = DateTime.Now;
                    isStockExist.IsDeleted = false;

                    ReturnText = $"Add {productName} (Replace deleted item) Success.";
                }
                else
                {
                    productLogger.LogWarning("Product name is duplicate.");
                    return BadRequest(new { Error = "Product name is duplicate." });
                }

                await context.SaveChangesAsync();
                return Ok(new { Success = $"Add {productName} Success." });
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("[action]")]
        public async Task<IActionResult> EditProduct([FromBody] ProductDTO_Post prodoctDTO)
        {
            try
            {
                var Product = await context.ProductLookUps.FindAsync(prodoctDTO.ProductId);

                if (Product != null)
                {
                    Product.ProductName = prodoctDTO.ProductName;

                    await context.SaveChangesAsync();
                    return Ok(new { Success = $"Edit {Product.ProductName} Success."});
                }
                else
                {
                    productLogger.LogWarning("Product Not Found.");
                    return NotFound("Product Not Found.");
                }
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }


        [HttpDelete("[action]")]
        public async Task<IActionResult> RemoveProduct(string productId)
        {
            try
            {
                var DeleteStock = await context.ProductLookUps.Where(x => x.ProductId.Equals(productId.Trim())).FirstOrDefaultAsync();

                if (DeleteStock != null)
                {
                    DeleteStock.IsDeleted = true;

                    await context.SaveChangesAsync();
                    return Ok(new { Success = $"Delete Product {DeleteStock.ProductName} Done." });
                }

                productLogger.LogWarning("Product not found.");
                return NotFound(new { Error = "Product not found" });
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}