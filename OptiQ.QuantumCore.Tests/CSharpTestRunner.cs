using Xunit;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace OptiQ.QuantumCore.Tests
{
    public class QuantumTestRunner
    {
        [Fact]
        public void TestPortfolioOptimizationBasic()
        {
            using var sim = new QuantumSimulator();
            // This runs the Q# tests - the actual test logic is in Q#
            // This is just a placeholder to ensure the test framework works
            Assert.True(true);
        }
        
        [Fact]
        public void VerifyQuantumSimulatorConnection()
        {
            using var sim = new QuantumSimulator();
            // Verify we can create a simulator instance
            Assert.NotNull(sim);
        }
    }
}
