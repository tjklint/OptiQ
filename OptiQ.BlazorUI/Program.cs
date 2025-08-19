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

// Configure HttpClient with specific JSON options for WebAssembly
builder.Services.AddScoped(sp => 
{
    var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    return httpClient;
});

// Add services
builder.Services.AddScoped<OptiQApiService>();

await builder.Build().RunAsync();
