using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OptiQ.BlazorUI.Components;
using OptiQ.BlazorUI.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base URL for production
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7200";

// Configure HttpClient with WebAssembly-compatible settings
builder.Services.AddScoped(sp => 
{
    var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    return httpClient;
});

// Configure JSON serialization options for WebAssembly compatibility
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.PropertyNameCaseInsensitive = true;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    options.WriteIndented = false;
});

// Add services
builder.Services.AddScoped<OptiQApiService>();

// Add logging
builder.Services.AddLogging();

await builder.Build().RunAsync();
