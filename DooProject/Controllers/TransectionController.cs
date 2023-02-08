using DooProject.Datas;
using DooProject.DTO;
using DooProject.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DooProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransectionController : ControllerBase
    {
        private readonly DatabaseContext context;
        private readonly ILogger transectionLogger;

        public TransectionController(DatabaseContext context, ILoggerFactory logger)
        {
            this.context = context;
            transectionLogger = logger.CreateLogger<TransectionController>();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetTransection()
        {
            try
            {
                return Ok(await context.ProductTransections
                    .Include(x => x.ProductLookUp)
                    .Where(x => !x.ProductLookUp.IsDeleted)
                    .Select(x => new { x.TransectionID, x.ProductLookUp.ProductId, x.ProductLookUp.ProductName, x.TransectionAmount })
                    .ToListAsync()
                    );
            }
            catch (Exception ex) 
            {
                transectionLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("[action]/{productId}")]
        public async Task<IActionResult> GetTransection(int productId)
        {
            try
            {
                return Ok(await context.ProductTransections
                    .Include(x => x.ProductLookUp)
                    .Where(x => !x.ProductLookUp.IsDeleted && x.ProductLookUp.ProductId.Equals(productId))
                    .Select(x => new { x.TransectionID, x.ProductLookUp.ProductId, x.ProductLookUp.ProductName, x.TransectionAmount })
                    .ToListAsync()
                    );
            }
            catch (Exception ex)
            {
                transectionLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AddTransection([FromBody] TransectionDTO transection)
        {
            try
            {
                var LookupProduct = await context.ProductLookUps.FindAsync(transection.ProductID);

                if (LookupProduct != null)
                {
                    await context.ProductTransections.AddAsync(new ProductTransection
                    {
                        ProductLookUp = LookupProduct,
                        TransectionAmount = transection.TransectionAmoung
                    });
                }
                else
                {
                    transectionLogger.LogError("Error : Product not found");
                    return NotFound(new { Error = "Product not found" });
                }

                await context.SaveChangesAsync();
                return Ok(new { Success = $"Add Transection done." });
            }
            catch(Exception ex)
            {
                transectionLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
