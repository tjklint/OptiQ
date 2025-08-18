using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using OptiQ.QuantumCore;

namespace OptiQ.Functions;

public class PortfolioFunction
{
    private readonly ILogger _logger;

    public PortfolioFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PortfolioFunction>();
    }

    [Function("Health")]
    public HttpResponseData Health([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "portfolio/health")] HttpRequestData req)
    {
        _logger.LogInformation("Health check requested");
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteString(JsonSerializer.Serialize(new { status = "healthy", timestamp = DateTime.UtcNow }));
        return response;
    }

    [Function("GetSamplePortfolio")]
    public HttpResponseData GetSamplePortfolio([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "portfolio/sample")] HttpRequestData req)
    {
        _logger.LogInformation("Sample portfolio requested");
        var sampleData = PortfolioUtils.GenerateSamplePortfolio();
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteString(JsonSerializer.Serialize(sampleData));
        return response;
    }

    [Function("GetRandomParameters")]
    public HttpResponseData GetRandomParameters([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "portfolio/parameters/random")] HttpRequestData req)
    {
        _logger.LogInformation("Random parameters requested");
        
        // Parse query parameters
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        int layers = int.TryParse(query["layers"], out var l) ? l : 2;
        int samples = int.TryParse(query["samples"], out var s) ? s : 100;
        
        var randomParams = PortfolioUtils.GenerateRandomQAOAParameters(layers, samples);
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteString(JsonSerializer.Serialize(randomParams));
        return response;
    }

    [Function("OptimizePortfolio")]
    public async Task<HttpResponseData> OptimizePortfolio([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "portfolio/optimize")] HttpRequestData req)
    {
        _logger.LogInformation("Portfolio optimization requested");
        
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<PortfolioOptimizationRequest>(requestBody);
            
            if (request?.PortfolioData == null || request.QAOAParameters == null)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.WriteString("Invalid request data");
                return errorResponse;
            }

            // Validate portfolio data
            if (!PortfolioUtils.ValidatePortfolioData(request.PortfolioData))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.WriteString("Invalid portfolio data");
                return errorResponse;
            }

            // Run quantum optimization (simulated)
            var result = await QuantumPortfolioOptimizer.OptimizePortfolioAsync(
                request.PortfolioData, 
                request.QAOAParameters);

            var optimizationResponse = new PortfolioOptimizationResponse
            {
                BestBitstring = result.BestBitstring,
                SelectedAssets = result.SelectedAssets,
                ExpectedReturn = result.ExpectedReturn,
                Risk = result.Risk,
                Cost = result.Cost,
                SampleCount = result.SampleCount,
                OptimizationTime = DateTime.UtcNow,
                Status = "Success"
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(optimizationResponse));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during portfolio optimization");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.WriteString($"Optimization failed: {ex.Message}");
            return errorResponse;
        }
    }
}

// Request/Response models for Azure Functions
public class PortfolioOptimizationRequest
{
    public PortfolioDataDto PortfolioData { get; set; } = new();
    public QAOAParametersDto QAOAParameters { get; set; } = new();
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
}
