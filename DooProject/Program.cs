using DooProject.Datas;
using DooProject.Interfaces;
using DooProject.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add CORS
var DooCors = "DooProjectCors";

// Add Dependency Injections
builder.Services.AddScoped<IProductServices, ProductServices>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: DooCors, policy => 
    {
        // Set Specific Origin to connect
        //policy.WithOrigins("http://127.0.0.1:5173", "http://localhost:5173")
        //.AllowAnyHeader()
        //.AllowAnyMethod();

        // Set allow all Origin to connect
        policy.AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(origin => true);
    });
});

// Database Context Dependencby Injection 
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

//// Add Authorization ( Add Policy )
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

// Add IgnoreCycles Protection
//builder.Services.AddControllers()
//    .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();


// Use Serilog
//builder.Host.UseSerilog((context, config) =>
//{
//    // Config from appsetting 
//    config.ReadFrom.Configuration(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build());

//    // Dirty Manual config
//    //config.WriteTo.Console();
//    //config.WriteTo.File(builder.Configuration.GetValue<string>("") ?? string.Empty);
//});

//builder.Services.AddSwaggerGen();

// Update Swagger for authentications and Authorization
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Set App to use DooCors Setup option
// and apply it to every request
app.UseCors(DooCors);

// Use Serilog
//app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
