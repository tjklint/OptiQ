namespace OptiQ.QuantumCore {
    
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Random;
    
    /// Portfolio data structure for optimization
    newtype PortfolioData = (
        AssetReturns: Double[],
        RiskMatrix: Double[][],
        AssetNames: String[],
        Budget: Double,
        RiskTolerance: Double
    );
    
    /// QAOA parameters for the optimization
    newtype QAOAParameters = (
        Layers: Int,
        BetaAngles: Double[],
        GammaAngles: Double[],
        Samples: Int
    );
    
    /// Portfolio optimization result
    newtype PortfolioResult = (
        BestBitstring: Bool[],
        SelectedAssets: String[],
        ExpectedReturn: Double,
        Risk: Double,
        Cost: Double,
        SampleCount: Int
    );
    
    /// Convert portfolio optimization to QUBO matrix
    function BuildQUBOMatrix(portfolioData: PortfolioData) : Double[][] {
        let assetCount = Length(portfolioData::AssetReturns);
        mutable quboMatrix : Double[][] = [];
        
        // Initialize matrix
        for i in 0..assetCount-1 {
            mutable row : Double[] = [];
            for j in 0..assetCount-1 {
                set row += [0.0];
            }
            set quboMatrix += [row];
        }
        
        // Add expected return terms (diagonal)
        for i in 0..assetCount-1 {
            mutable newRow = quboMatrix[i];
            set newRow w/= i <- -portfolioData::AssetReturns[i];
            set quboMatrix w/= i <- newRow;
        }
        
        // Add risk penalty terms (off-diagonal and diagonal)
        let riskPenalty = portfolioData::RiskTolerance;
        for i in 0..assetCount-1 {
            for j in 0..assetCount-1 {
                mutable newRow = quboMatrix[i];
                if i == j {
                    set newRow w/= j <- newRow[j] + riskPenalty * portfolioData::RiskMatrix[i][j];
                } else {
                    set newRow w/= j <- newRow[j] + 2.0 * riskPenalty * portfolioData::RiskMatrix[i][j];
                }
                set quboMatrix w/= i <- newRow;
            }
        }
        
        return quboMatrix;
    }
    
    /// Convert QUBO to Ising model parameters
    function QUBOToIsing(quboMatrix: Double[][]) : (Double[], Double[][]) {
        let n = Length(quboMatrix);
        mutable h : Double[] = [];
        mutable J : Double[][] = [];
        
        // Initialize arrays
        for i in 0..n-1 {
            set h += [0.0];
            mutable jRow : Double[] = [];
            for j in 0..n-1 {
                set jRow += [0.0];
            }
            set J += [jRow];
        }
        
        // Convert QUBO Q_ij to Ising h_i and J_ij
        for i in 0..n-1 {
            // Diagonal terms contribute to local fields
            set h w/= i <- quboMatrix[i][i] / 2.0;
            
            // Off-diagonal terms contribute to interactions
            for j in i+1..n-1 {
                mutable jRowI = J[i];
                mutable jRowJ = J[j];
                set jRowI w/= j <- quboMatrix[i][j] / 4.0;
                set jRowJ w/= i <- quboMatrix[i][j] / 4.0;
                set J w/= i <- jRowI;
                set J w/= j <- jRowJ;
                
                // Off-diagonal terms also contribute to local fields
                set h w/= i <- h[i] + quboMatrix[i][j] / 4.0;
                set h w/= j <- h[j] + quboMatrix[i][j] / 4.0;
            }
        }
        
        return (h, J);
    }
    
    /// Apply mixing Hamiltonian (X rotation on all qubits)
    operation ApplyMixer(qubits: Qubit[], beta: Double) : Unit is Adj + Ctl {
        for qubit in qubits {
            Rx(2.0 * beta, qubit);
        }
    }
    
    /// Apply cost Hamiltonian based on Ising model
    operation ApplyCostHamiltonian(qubits: Qubit[], h: Double[], J: Double[][], gamma: Double) : Unit is Adj + Ctl {
        let n = Length(qubits);
        
        // Apply local field terms
        for i in 0..n-1 {
            Rz(2.0 * gamma * h[i], qubits[i]);
        }
        
        // Apply interaction terms
        for i in 0..n-1 {
            for j in i+1..n-1 {
                if AbsD(J[i][j]) > 1e-10 {
                    CNOT(qubits[i], qubits[j]);
                    Rz(2.0 * gamma * J[i][j], qubits[j]);
                    CNOT(qubits[i], qubits[j]);
                }
            }
        }
    }
    
    /// Single QAOA layer
    operation QAOALayer(qubits: Qubit[], h: Double[], J: Double[][], gamma: Double, beta: Double) : Unit {
        ApplyCostHamiltonian(qubits, h, J, gamma);
        ApplyMixer(qubits, beta);
    }
    
    /// Initialize qubits in superposition
    operation InitializeSuperposition(qubits: Qubit[]) : Unit {
        for qubit in qubits {
            H(qubit);
        }
    }
    
    /// Measure all qubits and return results
    operation MeasureAllQubits(qubits: Qubit[]) : Bool[] {
        mutable results = [];
        for qubit in qubits {
            set results += [M(qubit) == One];
        }
        return results;
    }
    
    /// Calculate portfolio cost for a given bitstring
    function CalculatePortfolioCost(bitstring: Bool[], quboMatrix: Double[][]) : Double {
        let n = Length(bitstring);
        mutable cost = 0.0;
        
        for i in 0..n-1 {
            if bitstring[i] {
                set cost += quboMatrix[i][i];
                for j in i+1..n-1 {
                    if bitstring[j] {
                        set cost += quboMatrix[i][j];
                    }
                }
            }
        }
        
        return cost;
    }
    
    /// Calculate expected return for a portfolio
    function CalculateExpectedReturn(bitstring: Bool[], assetReturns: Double[]) : Double {
        mutable totalReturn = 0.0;
        mutable assetCount = 0;
        
        for i in 0..Length(bitstring)-1 {
            if bitstring[i] {
                set totalReturn += assetReturns[i];
                set assetCount += 1;
            }
        }
        
        return assetCount > 0 ? totalReturn / IntAsDouble(assetCount) | 0.0;
    }
    
    /// Calculate portfolio risk
    function CalculatePortfolioRisk(bitstring: Bool[], riskMatrix: Double[][]) : Double {
        let n = Length(bitstring);
        mutable risk = 0.0;
        mutable selectedCount = 0;
        
        for i in 0..n-1 {
            if bitstring[i] {
                set selectedCount += 1;
                for j in 0..n-1 {
                    if bitstring[j] {
                        set risk += riskMatrix[i][j];
                    }
                }
            }
        }
        
        return selectedCount > 0 ? risk / IntAsDouble(selectedCount * selectedCount) | 0.0;
    }
    
    /// Get selected asset names from bitstring
    function GetSelectedAssets(bitstring: Bool[], assetNames: String[]) : String[] {
        mutable selected = [];
        for i in 0..Length(bitstring)-1 {
            if bitstring[i] {
                set selected += [assetNames[i]];
            }
        }
        return selected;
    }
    
    /// Main QAOA portfolio optimization operation
    operation OptimizePortfolio(portfolioData: PortfolioData, qaoaParams: QAOAParameters) : PortfolioResult {
        let assetCount = Length(portfolioData::AssetReturns);
        let quboMatrix = BuildQUBOMatrix(portfolioData);
        let (h, J) = QUBOToIsing(quboMatrix);
        
        mutable bestCost = 1e10;
        mutable bestBitstring : Bool[] = [];
        for i in 0..assetCount-1 {
            set bestBitstring += [false];
        }
        mutable sampleResults = [];
        
        // Run QAOA sampling
        for sample in 1..qaoaParams::Samples {
            use qubits = Qubit[assetCount];
            
            // Initialize superposition
            InitializeSuperposition(qubits);
            
            // Apply QAOA layers
            for layer in 0..qaoaParams::Layers-1 {
                QAOALayer(qubits, h, J, qaoaParams::GammaAngles[layer], qaoaParams::BetaAngles[layer]);
            }
            
            // Measure and calculate cost
            let bitstring = MeasureAllQubits(qubits);
            let cost = CalculatePortfolioCost(bitstring, quboMatrix);
            
            set sampleResults += [(bitstring, cost)];
            
            if cost < bestCost {
                set bestCost = cost;
                set bestBitstring = bitstring;
            }
            
            ResetAll(qubits);
        }
        
        // Calculate final metrics for best solution
        let expectedReturn = CalculateExpectedReturn(bestBitstring, portfolioData::AssetReturns);
        let risk = CalculatePortfolioRisk(bestBitstring, portfolioData::RiskMatrix);
        let selectedAssets = GetSelectedAssets(bestBitstring, portfolioData::AssetNames);
        
        return PortfolioResult(
            bestBitstring,
            selectedAssets,
            expectedReturn,
            risk,
            bestCost,
            qaoaParams::Samples
        );
    }
    
    /// Generate random QAOA parameters for testing
    operation GenerateRandomQAOAParameters(layers: Int, samples: Int) : QAOAParameters {
        mutable betas = [];
        mutable gammas = [];
        
        for i in 0..layers-1 {
            set betas += [DrawRandomDouble(0.0, PI())];
            set gammas += [DrawRandomDouble(0.0, 2.0 * PI())];
        }
        
        return QAOAParameters(layers, betas, gammas, samples);
    }
    
    /// Optimize QAOA parameters using grid search
    operation OptimizeQAOAParameters(portfolioData: PortfolioData, layers: Int, gridSize: Int, samples: Int) : QAOAParameters {
        mutable bestParams = GenerateRandomQAOAParameters(layers, samples);
        mutable bestCost = 1e10;
        
        let stepSize = PI() / IntAsDouble(gridSize);
        
        for betaStep in 0..gridSize-1 {
            for gammaStep in 0..gridSize-1 {
                mutable betas = [];
                mutable gammas = [];
                
                for layer in 0..layers-1 {
                    set betas += [IntAsDouble(betaStep) * stepSize];
                    set gammas += [IntAsDouble(gammaStep) * stepSize];
                }
                
                let testParams = QAOAParameters(layers, betas, gammas, samples);
                let result = OptimizePortfolio(portfolioData, testParams);
                
                if result::Cost < bestCost {
                    set bestCost = result::Cost;
                    set bestParams = testParams;
                }
            }
        }
        
        return bestParams;
    }
}
