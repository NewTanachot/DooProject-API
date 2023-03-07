using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DooProject.Interfaces
{
    public interface IAuthServices
    {
        bool CheckIdClaimExist(List<Claim> userClaims, out string userId);
        Task<JwtSecurityToken?> CreateAccessTokenAsync(IdentityUser user);
        Task<IdentityUser?> FindUserAsync(string userId);
    }
}