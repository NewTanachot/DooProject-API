using Azure;
using Azure.Core;
using DooProject.Datas;
using DooProject.DTO;
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
        private readonly DatabaseContext context;
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ILoggerFactory logger;
        private readonly IConfiguration configuration;
        private readonly ILogger authLogger;

        public AuthController(DatabaseContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ILoggerFactory logger, IConfiguration configuration)
        {
            this.context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.logger = logger;
            this.configuration = configuration;
            authLogger = logger.CreateLogger<AuthController>();
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
                    UserName = registerInfo.Email,
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

                    // Create and Add Role "User" to User
                    // Can refactor this to another API in RoleController
                    if (!await roleManager.RoleExistsAsync("User"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("User"));
                    }

                    await userManager.AddToRoleAsync(user, "User");

                    return Ok(new { Success = $"User {registerInfo.Email} registered." });
                }

                authLogger.LogWarning($"Error : Something wrong.");
                return BadRequest(new { Error = $"Something wrong." });
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
                    var AuthService = new AuthServices(userManager, roleManager, logger, configuration);
                    var JwtToken = await AuthService.CreateAccessTokenAsync(User);

                    if (JwtToken != null)
                    {
                        return Ok(new { 
                            UserId = User.Id,
                            User.UserName,
                            AccessToken = new JwtSecurityTokenHandler().WriteToken(JwtToken),
                            AccessToken_CreateAt = JwtToken.ValidFrom.ToLocalTime(),
                            AccessToken_ExpireAt = JwtToken.ValidTo.ToLocalTime()
                        });
                    }
                }

                authLogger.LogWarning($"{loginInfo.Email} login fail.");
                return BadRequest(new { Error = $"{loginInfo.Email} login fail." });
            }
            catch(Exception ex) 
            {
                authLogger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
