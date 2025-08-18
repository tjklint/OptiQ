using System.Text.Json;

namespace OptiQ.QuantumCore
{
    /// <summary>
    /// Portfolio data for optimization
    /// </summary>
    public class PortfolioDataDto
    {
        public double[] AssetReturns { get; set; } = [];
        public double[][] RiskMatrix { get; set; } = [];
        public string[] AssetNames { get; set; } = [];
        public double Budget { get; set; }
        public double RiskTolerance { get; set; }
    }

    /// <summary>
    /// QAOA parameters for optimization
    /// </summary>
    public class QAOAParametersDto
    {
        public int Layers { get; set; }
        public double[] BetaAngles { get; set; } = [];
        public double[] GammaAngles { get; set; } = [];
        public int Samples { get; set; }
    }

    /// <summary>
    /// Portfolio optimization result
    /// </summary>
    public class PortfolioResultDto
    {
        public bool[] BestBitstring { get; set; } = [];
        public string[] SelectedAssets { get; set; } = [];
        public double ExpectedReturn { get; set; }
        public double Risk { get; set; }
        public double Cost { get; set; }
        public int SampleCount { get; set; }
    }

    /// <summary>
    /// Utility methods for portfolio optimization
    /// </summary>
    public static class PortfolioUtils
    {
        /// <summary>
        /// Generate sample portfolio data for testing
        /// </summary>
        public static PortfolioDataDto GenerateSamplePortfolio()
        {
            var random = new Random();
            var assetCount = 6;
            
            var assetNames = new[] { "AAPL", "GOOGL", "MSFT", "AMZN", "TSLA", "NVDA" };
            var assetReturns = Enumerable.Range(0, assetCount)
                .Select(_ => random.NextDouble() * 0.2 + 0.05) // Returns between 5% and 25%
                .ToArray();

            // Generate a positive definite covariance matrix for risk
            var riskMatrix = new double[assetCount][];
            var variances = new double[assetCount];
            
            // First, set up the diagonal elements (variances)
            for (int i = 0; i < assetCount; i++)
            {
                riskMatrix[i] = new double[assetCount];
                variances[i] = random.NextDouble() * 0.05 + 0.01; // Variance between 1% and 6%
                riskMatrix[i][i] = variances[i];
            }
            
            // Then set the off-diagonal elements (covariances)
            for (int i = 0; i < assetCount; i++)
            {
                for (int j = 0; j < assetCount; j++)
                {
                    if (i != j)
                    {
                        // Correlation coefficient between -0.5 and 0.5
                        var correlation = (random.NextDouble() - 0.5);
                        riskMatrix[i][j] = correlation * Math.Sqrt(variances[i] * variances[j]);
                    }
                }
            }

            return new PortfolioDataDto
            {
                AssetReturns = assetReturns,
                RiskMatrix = riskMatrix,
                AssetNames = assetNames,
                Budget = 1000000.0, // $1M budget
                RiskTolerance = 0.5
            };
        }

        /// <summary>
        /// Generate random QAOA parameters
        /// </summary>
        public static QAOAParametersDto GenerateRandomQAOAParameters(int layers, int samples)
        {
            var random = new Random();
            var betas = Enumerable.Range(0, layers).Select(_ => random.NextDouble() * Math.PI).ToArray();
            var gammas = Enumerable.Range(0, layers).Select(_ => random.NextDouble() * 2 * Math.PI).ToArray();
            
            return new QAOAParametersDto
            {
                Layers = layers,
                BetaAngles = betas,
                GammaAngles = gammas,
                Samples = samples
            };
        }

        /// <summary>
        /// Validate portfolio data
        /// </summary>
        public static bool ValidatePortfolioData(PortfolioDataDto data)
        {
            if (data.AssetReturns.Length != data.AssetNames.Length) return false;
            if (data.RiskMatrix.Length != data.AssetReturns.Length) return false;
            if (data.RiskMatrix.Any(row => row.Length != data.AssetReturns.Length)) return false;
            if (data.Budget <= 0) return false;
            if (data.RiskTolerance < 0) return false;
            
            return true;
        }

        /// <summary>
        /// Validate QAOA parameters
        /// </summary>
        public static bool ValidateQAOAParameters(QAOAParametersDto parameters)
        {
            if (parameters.Layers <= 0) return false;
            if (parameters.Samples <= 0) return false;
            if (parameters.BetaAngles.Length != parameters.Layers) return false;
            if (parameters.GammaAngles.Length != parameters.Layers) return false;
            
            return true;
        }
    }
}
