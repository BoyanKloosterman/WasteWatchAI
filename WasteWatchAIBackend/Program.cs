using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WasteWatchAIBackend.Data;
using WasteWatchAIBackend.Interface;
using WasteWatchAIBackend.Repository;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var DefaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Authentication setup
var jwtKey = builder.Configuration["Jwt:Key"] ?? builder.Configuration["JwtSettings:SecretKey"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<WasteWatchDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();
builder.Services.AddHttpClient();

// Add Identity for user management (needed for UserManager/SignInManager)
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<WasteWatchDbContext>()
    .AddDefaultTokenProviders();

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

// Generate and log JWT tokens for mock-api and fast-api on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var jwtServiceKey = builder.Configuration["Jwt:ServiceKey"] ?? jwtKey;
    var serviceIssuer = jwtIssuer;
    var serviceAudience = jwtAudience;
    var mockApiKey = builder.Configuration["ApiKeys:MockAPI"];
    var fastApiKey = builder.Configuration["ApiKeys:FastAPI"];
    var serviceAccountPassword = builder.Configuration["ServiceAccount:Password"];

    if (string.IsNullOrWhiteSpace(mockApiKey))
        logger.LogWarning("ApiKeys:MockAPI is missing or empty! JWT for mock-api will not be generated.");
    if (string.IsNullOrWhiteSpace(fastApiKey))
        logger.LogWarning("ApiKeys:FastAPI is missing or empty! JWT for fast-api will not be generated.");
    if (string.IsNullOrWhiteSpace(jwtServiceKey))
        logger.LogWarning("Jwt:ServiceKey (or fallback Jwt:Key) is missing or empty! Service JWTs may not be generated correctly.");

    string GenerateServiceToken(string username, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException($"JWT secret key for {username} is null or empty.");
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username),
                new System.Security.Claims.Claim("service", "true")
            }),
            Expires = DateTime.UtcNow.AddYears(1),
            Issuer = serviceIssuer,
            Audience = serviceAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    if (!string.IsNullOrWhiteSpace(mockApiKey))
    {
        var mockApiToken = GenerateServiceToken("mock-api", mockApiKey);
        logger.LogInformation($"Mock-API JWT: {mockApiToken}");
    }
    if (!string.IsNullOrWhiteSpace(fastApiKey))
    {
        var fastApiToken = GenerateServiceToken("fast-api", fastApiKey);
        logger.LogInformation($"Fast-API JWT: {fastApiToken}");
    }
}

app.Run();