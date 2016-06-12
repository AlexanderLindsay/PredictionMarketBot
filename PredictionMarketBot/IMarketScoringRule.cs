using System.Collections.Generic;

namespace PredictionMarketBot
{
    public interface IMarketScoringRule
    {
        double Cost(IEnumerable<int> holdings, double liquidity);

        double CalculateChange(IEnumerable<int> beginningHoldings, IEnumerable<int> endingHoldings, double liquidity);

        IEnumerable<double> CurrentPrices(IEnumerable<int> holdings, double liquidity);

        IEnumerable<double> Probabilities(IEnumerable<int> holdings, double liquidity);

    }
}