using Microsoft.AspNetCore.Mvc;
using OptiQ.QuantumCore;

namespace OptiQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(ILogger<PortfolioController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Optimize a portfolio using QAOA quantum algorithm (Simulated for now)
    /// </summary>
    [HttpPost("optimize")]
    public async Task<ActionResult<PortfolioOptimizationResponse>> OptimizePortfolio(
        [FromBody] PortfolioOptimizationRequest request)
    {
        try
        {
            _logger.LogInformation("Starting portfolio optimization with {AssetCount} assets", 
                request.PortfolioData.AssetNames.Length);

            // Validate input
            if (!PortfolioUtils.ValidatePortfolioData(request.PortfolioData))
            {
                return BadRequest("Invalid portfolio data");
            }

            if (!PortfolioUtils.ValidateQAOAParameters(request.QAOAParameters))
            {
                return BadRequest("Invalid QAOA parameters");
            }

            // For now, simulate quantum optimization with classical optimization
            // In a real implementation, this would call the Q# operations
            var result = await SimulateQuantumOptimization(request.PortfolioData, request.QAOAParameters);

            _logger.LogInformation("Portfolio optimization completed. Selected {SelectedCount} assets with expected return {Return:F4}", 
                result.SelectedAssets.Length, result.ExpectedReturn);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during portfolio optimization");
            return StatusCode(500, "Internal server error during optimization");
        }
    }

    /// <summary>
    /// Simulate quantum optimization for demonstration purposes
    /// </summary>
    private async Task<PortfolioOptimizationResponse> SimulateQuantumOptimization(
        PortfolioDataDto portfolioData, 
        QAOAParametersDto qaoaParameters)
    {
        // Simulate processing delay
        await Task.Delay(100);

        var random = new Random();
        var assetCount = portfolioData.AssetNames.Length;
        
        // Generate a reasonable portfolio selection (select 2-4 assets)
        var selectedAssetCount = Math.Min(4, Math.Max(2, assetCount / 2));
        var bestBitstring = new bool[assetCount];
        var selectedIndices = Enumerable.Range(0, assetCount)
            .OrderBy(_ => random.NextDouble())
            .Take(selectedAssetCount)
            .ToList();

        foreach (var index in selectedIndices)
        {
            bestBitstring[index] = true;
        }

        var selectedAssets = selectedIndices.Select(i => portfolioData.AssetNames[i]).ToArray();
        var expectedReturn = selectedIndices.Average(i => portfolioData.AssetReturns[i]);
        
        // Calculate risk as average covariance
        var risk = 0.0;
        var pairCount = 0;
        for (int i = 0; i < selectedIndices.Count; i++)
        {
            for (int j = 0; j < selectedIndices.Count; j++)
            {
                risk += portfolioData.RiskMatrix[selectedIndices[i]][selectedIndices[j]];
                pairCount++;
            }
        }
        risk = pairCount > 0 ? risk / pairCount : 0.0;

        // Simulate cost function (negative expected return + risk penalty)
        var cost = -expectedReturn + portfolioData.RiskTolerance * risk;

        return new PortfolioOptimizationResponse
        {
            BestBitstring = bestBitstring,
            SelectedAssets = selectedAssets,
            ExpectedReturn = expectedReturn,
            Risk = risk,
            Cost = cost,
            SampleCount = qaoaParameters.Samples,
            OptimizationTime = DateTime.UtcNow,
            Status = "Success (Simulated)"
        };
    }

    /// <summary>
    /// Generate sample portfolio data for testing
    /// </summary>
    [HttpGet("sample")]
    public ActionResult<PortfolioDataDto> GetSamplePortfolio()
    {
        var sampleData = PortfolioUtils.GenerateSamplePortfolio();
        return Ok(sampleData);
    }

    /// <summary>
    /// Generate random QAOA parameters for testing
    /// </summary>
    [HttpGet("parameters/random")]
    public ActionResult<QAOAParametersDto> GetRandomParameters(
        [FromQuery] int layers = 2, 
        [FromQuery] int samples = 100)
    {
        if (layers <= 0 || layers > 10)
        {
            return BadRequest("Layers must be between 1 and 10");
        }

        if (samples <= 0 || samples > 10000)
        {
            return BadRequest("Samples must be between 1 and 10000");
        }

        var parameters = PortfolioUtils.GenerateRandomQAOAParameters(layers, samples);
        return Ok(parameters);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow, Service = "OptiQ Portfolio Optimizer" });
    }
}

/// <summary>
/// Request model for portfolio optimization
/// </summary>
public class PortfolioOptimizationRequest
{
    public PortfolioDataDto PortfolioData { get; set; } = new();
    public QAOAParametersDto QAOAParameters { get; set; } = new();
}

/// <summary>
/// Response model for portfolio optimization
/// </summary>
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
