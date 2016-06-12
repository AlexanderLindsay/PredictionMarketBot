using System;
using System.Collections.Generic;
using System.Linq;

namespace PredictionMarketBot
{
    public class LogarithmicMarketScoringRule : IMarketScoringRule
    {
        private static readonly string LiqudityException = "Liquidty should be greater than zero";
        private static readonly string HoldingsException = "Holdings can't be null";
        private static readonly string HoldingException = "Each holding should be greater than or equal to zero";

        public double Cost(IEnumerable<int> holdings, double liquidity)
        {
            if (liquidity <= 0)
                throw new ArgumentOutOfRangeException(LiqudityException);

            if (holdings == null)
                throw new ArgumentNullException(HoldingsException);

            var sum = holdings.Aggregate(0.0, (accum, holding) =>
            {
                if (holding < 0)
                    throw new ArgumentOutOfRangeException(HoldingException);

                return accum + Math.Exp(holding / liquidity);
            });

            return liquidity * Math.Log(sum);
        }

        public double CalculateChange(IEnumerable<int> beginningHoldings, IEnumerable<int> endingHoldings, double liquidity)
        {
            if (liquidity <= 0)
                throw new ArgumentOutOfRangeException(LiqudityException);

            if (beginningHoldings == null)
                throw new ArgumentNullException("Beginning " + HoldingsException);

            if (endingHoldings == null)
                throw new ArgumentNullException("Ending " + HoldingsException);

            var startingCost = Cost(beginningHoldings, liquidity);
            var endingCost = Cost(endingHoldings, liquidity);
            return endingCost - startingCost;
        }

        public IEnumerable<double> CurrentPrices(IEnumerable<int> holdings, double liquidity)
        {
            if (liquidity <= 0)
                throw new ArgumentOutOfRangeException(LiqudityException);

            if (holdings == null)
                throw new ArgumentNullException(HoldingsException);

            return holdings.Select((holding, index) =>
            {
                var array = holdings.ToArray();
                array[index]++;
                return CalculateChange(holdings, array, liquidity);
            });
        }

        public IEnumerable<double> Probabilities(IEnumerable<int> holdings, double liquidity)
        {
            if (liquidity <= 0)
                throw new ArgumentOutOfRangeException(LiqudityException);

            if (holdings == null)
                throw new ArgumentNullException(HoldingsException);

            var denom = 0.0;
            foreach(var holding in holdings)
            {
                denom += Math.Exp(holding / liquidity);
            }

            return holdings.Select(holding =>
            {
                return Math.Exp(holding / liquidity) / denom;
            });
        }
    }
}
