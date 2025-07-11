using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using WasteWatchAIFrontend.Components;
using WasteWatchAIFrontend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add authentication services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Add HttpClient services
builder.Services.AddHttpClient();

// Configure a named HttpClient with base address
builder.Services.AddHttpClient("WasteWatchAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:8080/"); // Replace with your API base URL
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();