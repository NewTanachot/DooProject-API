using DooProject.Datas;
using DooProject.DTO;
using DooProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Immutable;

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
        public async Task<IActionResult> GetTransectionForAllUserAsync()
        {
            try
            {
                //var NotInclude = await context.ProductTransections.ToListAsync();
                //transectionLogger.LogInformation("");
                //var Include = await context.ProductTransections.ToListAsync();

                ////Console.WriteLine("NotInClude : " + JsonConvert.SerializeObject(NotInclude));
                ////Console.WriteLine("");
                ////Console.WriteLine("InClude : " + JsonConvert.SerializeObject(Include));

                //return Ok(new
                //{
                //    NotInclude,
                //    Include
                //});

                return Ok(await context.ProductTransections
                    .Include(x => x.ProductLookUp)
                    .ThenInclude(x => x.User)
                    .Where(x => !x.ProductLookUp.IsDeleted)
                    .Select(x =>
                        new
                        {
                            x.TransectionID,
                            x.ProductLookUp.User.Id,
                            x.ProductLookUp.User.UserName,
                            x.ProductLookUp.ProductId,
                            x.ProductLookUp.ProductName,
                            x.Quantity,
                            x.TransectionDate,
                            x.TransectionType
                        })
                    .OrderByDescending(x => x.TransectionDate)
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
        public async Task<IActionResult> GetTransectionForAllUserAsync(string productId)
        {
            try
            {
                return Ok(await context.ProductTransections
                    .Include(x => x.ProductLookUp)
                    .ThenInclude(x => x.User)
                    .Where(x => !x.ProductLookUp.IsDeleted && x.ProductLookUp.ProductId == productId)
                    .Select(x => new 
                        { 
                            x.TransectionID,
                            x.ProductLookUp.User.Id,
                            x.ProductLookUp.User.UserName,
                            x.ProductLookUp.ProductId, 
                            x.ProductLookUp.ProductName, 
                            x.Quantity, 
                            x.TransectionDate,
                            x.TransectionType 
                        })
                    .OrderByDescending(x => x.TransectionDate)
                    .ToListAsync()
                    );
            }
            catch (Exception ex)
            {
                transectionLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetTransectionAsync()
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.FirstOrDefault(x => x.Type == "Id");
                if (userId == null)
                {
                    transectionLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                return Ok(await context.ProductTransections
                    .Include(x => x.ProductLookUp)
                    .ThenInclude(x => x.User)
                    .Where(x => !x.ProductLookUp.IsDeleted && x.ProductLookUp.User.Id == userId.Value)
                    .Select(x => new 
                        {
                            x.TransectionID,
                            x.ProductLookUp.User.Id,
                            x.ProductLookUp.User.UserName,
                            x.ProductLookUp.ProductId,
                            x.ProductLookUp.ProductName,
                            x.Quantity,
                            x.TransectionDate,
                            x.TransectionType
                        })
                    .OrderByDescending (x => x.TransectionDate)
                    .ToListAsync()
                    );
            }
            catch (Exception ex)
            {
                transectionLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("[action]/{productId}")]
        public async Task<IActionResult> GetTransectionAsync(string productId)
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.FirstOrDefault(x => x.Type == "Id");
                if (userId == null)
                {
                    transectionLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                return Ok(await context.ProductTransections
                    .Include(x => x.ProductLookUp)
                    .ThenInclude(x => x.User)
                    .Where(x => !x.ProductLookUp.IsDeleted 
                        && x.ProductLookUp.User.Id == userId.Value
                        && x.ProductLookUp.ProductId == productId
                        )
                    .Select(x => new 
                        {
                            x.TransectionID,
                            x.ProductLookUp.User.Id,
                            x.ProductLookUp.User.UserName,
                            x.ProductLookUp.ProductId,
                            x.ProductLookUp.ProductName,
                            x.Quantity,
                            x.TransectionDate,
                            x.TransectionType
                        })
                    .OrderByDescending(x => x.TransectionDate)
                    .ToListAsync()
                    );
            }
            catch (Exception ex)
            {
                transectionLogger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> AddTransectionAsync([FromBody] TransectionDTO transection)
        {
            try
            {
                // Find userId in JWT  and  Check if it have Id Claim
                var userId = User.Claims.FirstOrDefault(x => x.Type == "Id");
                if (userId == null)
                {
                    transectionLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new 
                    { 
                        Error = "Invalid Token Structure (No UserId)." 
                    });
                }

                // Find Product by PK and Check if not exist
                var LookupProduct = await context.ProductLookUps.FindAsync(transection.ProductID);

                if (LookupProduct == null)
                {
                    transectionLogger.LogError("Error : Product not found");
                    return NotFound(new { Error = "Product not found" });
                }

                // Check if TransectionAmoung is not 0
                if (transection.Quantity != 0)
                {
                    // Add new Transection
                    await context.ProductTransections.AddAsync(new ProductTransection
                    {
                        ProductLookUp = LookupProduct,
                        Quantity = transection.Quantity,
                        TransectionType = transection.TransectionType
                    });
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
