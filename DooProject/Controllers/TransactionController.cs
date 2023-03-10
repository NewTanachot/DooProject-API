using DooProject.Datas;
using DooProject.DTO;
using DooProject.Interfaces;
using DooProject.Models;
using DooProject.Services;
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
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionServices transactionServices;
        private readonly IAuthServices authServices;
        private readonly IProductServices productServices;
        private readonly ILogger transactionLogger;

        public TransactionController(
            ITransactionServices transactionServices, 
            IAuthServices authServices, 
            IProductServices productServices, 
            ILogger<TransactionController> logger)
        {
            this.transactionServices = transactionServices;
            this.authServices = authServices;
            this.productServices = productServices;
            transactionLogger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetTransactionForAllUserAsync()
        {
            try
            {
                return Ok(await transactionServices.GetTransactionAsync());
            }
            catch (Exception ex) 
            {
                transactionLogger.LogError(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"{ex.Message} {Environment.NewLine} {ex.StackTrace}");
            }
        }

        [HttpGet("[action]/{productId}")]
        public async Task<IActionResult> GetTransactionForAllUserAsync(string productId)
        {
            try
            {
                return Ok(await transactionServices.GetTransactionAsync(productId));
            }
            catch (Exception ex)
            {
                transactionLogger.LogError(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"{ex.Message} {Environment.NewLine} {ex.StackTrace}");
            }
        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetTransactionAsync()
        {
            try
            {
                // Find userId in Http header  and  Check if it have Id Claim
                if (!authServices.CheckIdClaimExist(User.Claims.ToList(), out string userId))
                {
                    transactionLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                return Ok(await transactionServices.GetUserTransactionAsync(userId));
            }
            catch (Exception ex)
            {
                transactionLogger.LogError(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"{ex.Message} {Environment.NewLine} {ex.StackTrace}");
            }
        }

        [Authorize]
        [HttpGet("[action]/{productId}")]
        public async Task<IActionResult> GetTransactionAsync(string productId)
        {
            try
            {
                // Find userId in Http header  and  Check if it have Id Claim
                if (!authServices.CheckIdClaimExist(User.Claims.ToList(), out string userId))
                {
                    transactionLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                return Ok(await transactionServices.GetUserTransactionAsync(userId, productId));
            }
            catch (Exception ex)
            {
                transactionLogger.LogError(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"{ex.Message} {Environment.NewLine} {ex.StackTrace}");
            }
        }

        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> AddTransactionAsync([FromBody] TransactionDTO transaction)
        {
            try
            {
                // Find userId in Http header  and  Check if it have Id Claim
                if (!authServices.CheckIdClaimExist(User.Claims.ToList(), out string userId))
                {
                    transactionLogger.LogWarning("Invalid Token Structure (No UserId).");
                    return StatusCode(StatusCodes.Status403Forbidden, new { Error = "Invalid Token Structure (No UserId)." });
                }

                // Find Product by PK and Check if not exist
                var LookupProduct = await productServices.FindPoductByIdAsync(transaction.ProductID, false);

                if (LookupProduct == null)
                {
                    transactionLogger.LogError("Error : Product not found");
                    return NotFound(new { Error = "Product not found" });
                }

                // Add transaction by call AddTransaction method
                await transactionServices.AddTransactionAsync(LookupProduct, transaction.Quantity, transaction.TransactionType);

                return Ok(new { Success = $"Add Transaction done." });
            }
            catch(Exception ex)
            {
                transactionLogger.LogError(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"{ex.Message} {Environment.NewLine} {ex.StackTrace}");
            }
        }
    }
}
