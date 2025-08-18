using System.Net.Http.Json;
using OptiQ.QuantumCore;

namespace OptiQ.BlazorUI.Services;

public class OptiQApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OptiQApiService> _logger;
    private readonly string _baseUrl;

    public OptiQApiService(HttpClient httpClient, ILogger<OptiQApiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["OptiQApi:BaseUrl"] ?? "https://localhost:7001/api";
    }

    public async Task<PortfolioOptimizationResponse?> OptimizePortfolioAsync(
        PortfolioDataDto portfolioData, 
        QAOAParametersDto qaoaParameters)
    {
        try
        {
            var request = new PortfolioOptimizationRequest(portfolioData, qaoaParameters);
            
            _logger.LogInformation("Sending optimization request to API");
            
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/portfolio/optimize", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PortfolioOptimizationResponse>();
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
            
            var response = await _httpClient.GetFromJsonAsync<PortfolioDataDto>($"{_baseUrl}/portfolio/sample");
            
            _logger.LogInformation("Sample portfolio data retrieved");
            return response;
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
            
            var response = await _httpClient.GetFromJsonAsync<QAOAParametersDto>(
                $"{_baseUrl}/portfolio/parameters/random?layers={layers}&samples={samples}");
            
            _logger.LogInformation("Random QAOA parameters retrieved");
            return response;
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
            var response = await _httpClient.GetAsync($"{_baseUrl}/portfolio/health");
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
