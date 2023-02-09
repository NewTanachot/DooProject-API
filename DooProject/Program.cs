using DooProject.Datas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Database Context Dependency Injection 
builder.Services.AddDbContext<DatabaseContext>(options => {
    options.UseSqlite(builder.Configuration.GetConnectionString("Default"));
});

// Add Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(option =>
{
    //option.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

    // Essential Password Option
    option.Password.RequireNonAlphanumeric = false;
    option.Password.RequireDigit = false;
    option.Password.RequireUppercase = false;

    // Check duplicate Email
    option.User.RequireUniqueEmail = true;

}).AddEntityFrameworkStores<DatabaseContext>(); ;

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration.GetValue<string>("SecretKey") ?? "")),
        ValidateLifetime = true,
        ValidateAudience = false,
        ValidateIssuer = false,
        ClockSkew = TimeSpan.Zero,
    };
});

//// Add Authorixation ( Add Policy )
//builder.Services.AddAuthorization(option =>
//{
//    // need to Add IAuthorizationHandler
//    option.AddPolicy("AuthorizeAll", policy => {
//        policy.RequireClaim("Permission", "View");
//        //policy.Requirements.Add(new AdultOnly(18));
//    });
//});

//// Add IAuthorizationHandler for custom Handler
//builder.Services.AddSingleton<IAuthorizationHandler, AdultOnlyHandler>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
