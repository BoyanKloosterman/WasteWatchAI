using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using WasteWatchAIFrontend.Components;
using WasteWatchAIFrontend.Services.Auth;
using Microsoft.AspNetCore.Authentication.Cookies; // Add this

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add authentication services
// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    });


builder.Services.AddAuthorization();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Add HttpClient services
builder.Services.AddHttpClient();

// Configure a named HttpClient with base address
builder.Services.AddHttpClient("WasteWatchAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:8080/"); // Replace with your API base URL
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("FastAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:8000/"); // Replace with your FastAPI base URL
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication(); // Add this
app.UseAuthorization();  // Add this

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
