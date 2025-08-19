using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using OptiQ.QuantumCore;

namespace OptiQ.BlazorUI.Services;

public class OptiQApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OptiQApiService> _logger;
    
    // Use minimal JSON options for WebAssembly compatibility
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OptiQApiService(HttpClient httpClient, ILogger<OptiQApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PortfolioOptimizationResponse?> OptimizePortfolioAsync(
        PortfolioDataDto portfolioData, 
        QAOAParametersDto qaoaParameters)
    {
        try
        {
            var request = new PortfolioOptimizationRequest(portfolioData, qaoaParameters);
            
            _logger.LogInformation("Sending optimization request to API");
            
            // Use string-based serialization to avoid WebAssembly JSON issues
            var jsonString = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/portfolio/optimize", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PortfolioOptimizationResponse>(responseContent, _jsonOptions);
                _logger.LogInformation("Optimization completed successfully");
                return result;
            }
            else
            {
                _logger.LogError("API returned error: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling optimization API");
            return null;
        }
    }

    public async Task<PortfolioDataDto?> GetSamplePortfolioAsync()
    {
        try
        {
            _logger.LogInformation("Fetching sample portfolio data");
            
            var response = await _httpClient.GetAsync("api/portfolio/sample");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PortfolioDataDto>(responseContent, _jsonOptions);
                _logger.LogInformation("Sample portfolio data retrieved");
                return result;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sample portfolio");
            return null;
        }
    }

    public async Task<QAOAParametersDto?> GetRandomParametersAsync(int layers = 2, int samples = 100)
    {
        try
        {
            _logger.LogInformation("Fetching random QAOA parameters");
            
            var response = await _httpClient.GetAsync($"api/portfolio/parameters/random?layers={layers}&samples={samples}");
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<QAOAParametersDto>(responseContent, _jsonOptions);
                _logger.LogInformation("Random QAOA parameters retrieved");
                return result;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching random parameters");
            return null;
        }
    }

    public async Task<bool> CheckApiHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/portfolio/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

// Define the request/response models here as well for the Blazor UI
public class PortfolioOptimizationRequest
{
    public PortfolioDataDto PortfolioData { get; set; } = new();
    public QAOAParametersDto QAOAParameters { get; set; } = new();

    public PortfolioOptimizationRequest() { }

    public PortfolioOptimizationRequest(PortfolioDataDto portfolioData, QAOAParametersDto qaoaParameters)
    {
        PortfolioData = portfolioData;
        QAOAParameters = qaoaParameters;
    }
}

public class PortfolioOptimizationResponse
{
    public bool[] BestBitstring { get; set; } = [];
    public string[] SelectedAssets { get; set; } = [];
    public double ExpectedReturn { get; set; }
    public double Risk { get; set; }
    public double Cost { get; set; }
    public int SampleCount { get; set; }
    public DateTime OptimizationTime { get; set; }
    public string Status { get; set; } = "";
};
