using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Avans.Identity.Dapper;
using System.Data;
using WasteWatchAIBackend.Data;
using WasteWatchAIBackend.Interface;
using WasteWatchAIBackend.Repositories;
using WasteWatchAIBackend.Repository;
using WasteWatchAIBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddDapperStores(options =>
{
    options.ConnectionString = sqlConnectionString;
    //options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
});

builder.Services
    .AddAuthorization()
    .AddIdentityApiEndpoints<IdentityUser>();

builder.Services.AddDbContext<WasteWatchDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAuthenticationService, AspNetIdentityAuthenticationService>();
builder.Services.AddScoped<IDbConnection>(sp => new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();
builder.Services.AddScoped<IIdentityRepository, IdentityRepository>();

builder.Services.AddHttpClient();

builder.Services
    .AddOptions<BearerTokenOptions>()
    .Bind(builder.Configuration.GetSection("BearerToken"))
    .Configure(options =>
    {
        options.BearerTokenExpiration = TimeSpan.FromHours(1);
        options.RefreshTokenExpiration = TimeSpan.FromDays(7);
    });
builder.Services.AddTransient<IAuthenticationService, AspNetIdentityAuthenticationService>();

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
app.MapGroup("account")
   .MapIdentityApi<IdentityUser>();
app.MapPost("/account/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});
app.MapControllers();

app.Run();