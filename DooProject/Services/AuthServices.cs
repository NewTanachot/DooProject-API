using DooProject.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DooProject.Services
{
    public class AuthServices
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration configuration;
        private readonly ILogger authServiceLogger;

        public AuthServices(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ILoggerFactory logger, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.configuration = configuration;
            authServiceLogger = logger.CreateLogger<AuthController>();
        }

        public async Task<JwtSecurityToken?> CreateAccessTokenAsync(IdentityUser user)
        {
            try 
            {
                // Set JwtToken Expire TimeSpan (Minutes)
                int TokenExpireSpan = 1;

                var TokenClaims = new List<Claim>();

                // Add all UserClaims
                TokenClaims.AddRange(await userManager.GetClaimsAsync(user));

                // Get RoleClaims
                foreach (var roleName in await userManager.GetRolesAsync(user))
                {
                    // Add Role
                    TokenClaims.AddRange(new List<Claim> { new Claim(ClaimTypes.Role, roleName) });
                    var Role = await roleManager.FindByNameAsync(roleName);

                    if (Role != null)
                    {
                        // Add RoleClaim
                        TokenClaims.AddRange(await roleManager.GetClaimsAsync(Role));
                    }
                }

                // Distinct all duplicate Claim
                TokenClaims = TokenClaims.DistinctBy(x => (x.Value, x.Type)).ToList();

                var secretKey = Encoding.UTF8.GetBytes(configuration.GetValue<string>("SecretKey") ?? "");

                return new JwtSecurityToken(
                    claims: TokenClaims,
                    notBefore: DateTime.Now,
                    expires: DateTime.Now.AddMinutes(TokenExpireSpan),
                    signingCredentials: new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256)
                );
            }
            catch(Exception ex)
            {
                authServiceLogger.LogError(ex.Message);
                return null;
            }
        }
    }
}
