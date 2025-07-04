using Microsoft.EntityFrameworkCore;
using WasteWatchAIBackend.Data;
using WasteWatchAIBackend.Interface;
using Microsoft.AspNetCore.Identity;
using WasteWatchAIBackend.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<WasteWatchDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();
builder.Services.AddHttpClient();

    

builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; 
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
}).AddEntityFrameworkStores<WasteWatchDbContext>();
var app = builder.Build();

// API Key Middleware
// const string API_KEY_NAME = "X-API-KEY";
// var apiKey = builder.Configuration["ApiKey"];

// app.Use(async (context, next) =>
// {
//     if (string.IsNullOrEmpty(apiKey))
//     {
//         context.Response.StatusCode = 500;
//         await context.Response.WriteAsync("API Key is not configured on the server.");
//         return;
//     }
//     // Allow Swagger UI and static files without API key
//     var path = context.Request.Path.Value;
//     if (path.StartsWith("/swagger") || path.StartsWith("/favicon") || path.StartsWith("/_framework"))
//     {
//         await next();
//         return;
//     }
//     if (!context.Request.Headers.TryGetValue(API_KEY_NAME, out var extractedApiKey))
//     {
//         context.Response.StatusCode = 401;
//         await context.Response.WriteAsync("API Key was not provided.");
//         return;
//     }
//     if (!apiKey.Equals(extractedApiKey))
//     {
//         context.Response.StatusCode = 403;
//         await context.Response.WriteAsync("Unauthorized client.");
//         return;
//     }
//     await next();
// });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/account")
.MapIdentityApi<IdentityUser>();

app.MapControllers()
.RequireAuthorization();

app.Run();