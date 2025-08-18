using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OptiQ.BlazorUI.Components;
using OptiQ.BlazorUI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base URL for production
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7200";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Add services
builder.Services.AddScoped<OptiQApiService>();

await builder.Build().RunAsync();
