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
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Configure JSON serialization options for WebAssembly compatibility
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Add services
builder.Services.AddScoped<OptiQApiService>();

await builder.Build().RunAsync();
