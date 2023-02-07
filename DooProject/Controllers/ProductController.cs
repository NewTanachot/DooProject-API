using AutoMapper;
using DooProject.Datas;
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
            productLogger = logger.CreateLogger(typeof(ProductController));
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetStock()
        {
            try
            {
                var productsDTO = new List<ProdoctDTO_Get>();
                var Product = await context.ProductLookUps
                    .Where(x => !x.IsDeleted)
                    .Select(x => new { x.ProductId, x.ProductName, x.CreateTime })
                    .ToListAsync();

                Product.ForEach(x => productsDTO.Add(ProdoctDTO_Get.ProdoctDTOMapper(x.ProductId, x.ProductName, x.CreateTime)));

                return Ok(productsDTO);
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AddStock([FromBody] string ProductName)
        {
            try
            {
                var ReturnText = string.Empty;
                var isStockExist = await context.ProductLookUps.Where(x => x.ProductName.Equals(ProductName.Trim())).FirstOrDefaultAsync();

                if (isStockExist == null)
                {
                    await context.ProductLookUps.AddAsync(new ProductLookUp
                    {
                        ProductName = ProductName.Trim()
                    });

                    ReturnText = $"Add {ProductName} Success.";
                }
                else if (isStockExist.IsDeleted)
                {
                    isStockExist.ProductName = ProductName.Trim();
                    isStockExist.CreateTime = DateTime.Now;
                    isStockExist.IsDeleted = false;

                    ReturnText = $"Add {ProductName} (Replace deleted item) Success.";
                }
                else
                {
                    productLogger.LogWarning("Product name is duplicate.");
                    return BadRequest("Product name is duplicate.");
                }

                await context.SaveChangesAsync();
                return Ok($"Add {ProductName} Success.");
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("[action]")]
        public async Task<IActionResult> EditProduct([FromBody] ProductDTO_Post ProdoctDTO)
        {
            try
            {
                var Product = await context.ProductLookUps.FindAsync(ProdoctDTO.ProductId);

                if (Product != null)
                {
                    Product.ProductName = ProdoctDTO.ProductName;

                    await context.SaveChangesAsync();
                    return Ok($"Edit {Product.ProductName} Success.");
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
        public async Task<IActionResult> RemoveStock(string ProductId)
        {
            try
            {
                var DeleteStock = await context.ProductLookUps.Where(x => x.ProductId.Equals(ProductId.Trim())).FirstOrDefaultAsync();

                if (DeleteStock != null)
                {
                    DeleteStock.IsDeleted = true;

                    await context.SaveChangesAsync();
                    return Ok($"Delete Product {DeleteStock.ProductName} Done.");
                }

                productLogger.LogWarning("Product not found.");
                return NotFound("Product not found.");
            }
            catch (Exception ex)
            {
                productLogger.LogError(ex, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
