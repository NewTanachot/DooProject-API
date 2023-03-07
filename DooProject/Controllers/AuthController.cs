using Azure;
using Azure.Core;
using DooProject.Datas;
using DooProject.DTO;
using DooProject.Interfaces;
using DooProject.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DooProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthServices authServices;
        private readonly UserManager<IdentityUser> userManager;
        private readonly ILogger authLogger;

        public AuthController(IAuthServices authServices ,UserManager<IdentityUser> userManager, ILogger<AuthController> logger)
        {
            this.authServices = authServices;
            this.userManager = userManager;
            authLogger = logger;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerInfo)
        {
            try
            {
                // Create the User
                var user = new IdentityUser
                {
                    Email = registerInfo.Email,
                    UserName = registerInfo.FirstName,
                };

                var result = await userManager.CreateAsync(user, registerInfo.Password);

                if (result.Succeeded)
                {
                    // Add Claims to User
                    await userManager.AddClaimsAsync(user, new List<Claim>
                    {
                        new Claim("FirstName", registerInfo.FirstName),
                        new Claim("LastName", registerInfo.LastName)
                    });

                    return Ok(new { Success = $"User {registerInfo.Email} registered." });
                }

                // Log all Error Code
                result.Errors.ToList().ForEach(x =>
                {
                    authLogger.LogWarning(x.Code, x.Description);
                });

                return BadRequest(new { Error = result.Errors });
            }
            catch (Exception ex) 
            {
                authLogger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginInfo)
        {
            try
            {
                var User = await userManager.FindByEmailAsync(loginInfo.Email);

                if (User != null && await userManager.CheckPasswordAsync(User, loginInfo.Password))
                {
                    // Create JwtToken
                    var JwtToken = await authServices.CreateAccessTokenAsync(User);

                    // Check If token create fail
                    if (JwtToken == null)
                    {
                        throw new Exception("Fail to create JWToken.");
                    }

                    return Ok(new
                    {
                        UserId = User.Id,
                        User.UserName,
                        AccessToken = new JwtSecurityTokenHandler().WriteToken(JwtToken),
                        AccessToken_CreateAt = JwtToken.ValidFrom.ToLocalTime(),
                        AccessToken_ExpireAt = JwtToken.ValidTo.ToLocalTime()
                    });
                }

                authLogger.LogWarning($"{loginInfo.Email} login fail. Wrong password or User doesn't exist");
                return BadRequest(new { Error = $"{loginInfo.Email} login fail. Wrong password or User doesn't exist" });
            }
            catch(Exception ex) 
            {
                authLogger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
