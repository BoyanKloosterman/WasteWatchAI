using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WasteWatchAIBackend.Data;
using WasteWatchAIBackend.Interface;
using WasteWatchAIBackend.Repository;
using WasteWatchAIBackend.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Identity Framework
builder.Services.AddAuthorization(options =>
{
    // Voorbeeld policies
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireClaim("role", "admin"));
    options.AddPolicy("UserOrAdmin", policy => 
        policy.RequireClaim("role", "user", "admin"));
});

builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
{
    // Configureer wachtwoord complexiteit
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    // Email configuratie
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddDapperStores(options => 
        options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddDbContext<WasteWatchDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Identity Framework middleware (volgorde is belangrijk!)
app.UseAuthentication();
app.UseAuthorization();

// Map Identity API endpoints onder /account
app.MapGroup("/account").MapIdentityApi<IdentityUser>();

// Optioneel: logout endpoint
app.MapPost("/account/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok(new { Message = "Successfully logged out" });
}).RequireAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string serviceUserEmail = "serviceaccount@example.com";
    string serviceUserPassword = builder.Configuration["ServiceAccount:Password"] ?? "VeiligWachtwoord123!"; // haal uit config

    var user = await userManager.FindByEmailAsync(serviceUserEmail);
    if (user == null)
    {
        user = new IdentityUser
        {
            UserName = serviceUserEmail,
            Email = serviceUserEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, serviceUserPassword);
        if (!result.Succeeded)
        {
            // Log eventueel fouten hier
            throw new Exception("Kon service account niet aanmaken: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}

app.Run();

