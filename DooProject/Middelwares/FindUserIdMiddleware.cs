using DooProject.Interfaces;
using DooProject.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Threading.Tasks;
using System;

namespace DooProject.Middelwares
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class FindUserIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public FindUserIdMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // We can't use Service in DI container because we use addScope. it will cause dispose error.
            // We need to instantiate IAuthServices in middleware scope
            var scope = serviceScopeFactory.CreateScope();
            var authServices = scope.ServiceProvider.GetService<IAuthServices>();

            // Get all Claims in User from request
            var AllClaims = httpContext.User.Claims.ToList();

            // Check ID by method in authServices
            if (authServices != null && authServices.CheckIdClaimExist(AllClaims, out string UserId))
            {
                // Create Item Dictionary to access UserId in every endpoint
                httpContext.Items["UserId"] = UserId;

                // call next RequestDelegate pipline
                await _next(httpContext);
            }
            else
            {
                // Set StatusCode and ResponseMessage in response body
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                // Convert string error message to bytes code
                var bytesErrorMessage = Encoding.UTF8.GetBytes("Invalid Token Structure (No UserId).");
                await httpContext.Response.Body.WriteAsync(bytesErrorMessage);

                // Log warning to console
                await httpContext.Response.WriteAsync("Invalid Token Structure (No UserId).");
                return;
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class FindUserIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseFindUserIdMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FindUserIdMiddleware>();
        }
    }
}
