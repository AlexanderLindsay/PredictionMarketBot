using System;
using System.Collections.Generic;
using System.Linq;

namespace PredictionMarketBot
{
    public class LogarithmicMarketScoringRule : IMarketScoringRule
    {
        private static readonly string LiqudityException = "Liquidty should be greater than zero";
        private static readonly string HoldingsNullException = "Holdings can't be null";
        private static readonly string HoldingsEmptyException = "Holdings can't be null";
        private static readonly string HoldingException = "Each holding should be greater than or equal to zero";

        public double Cost(IEnumerable<int> holdings, double liquidity)
        {
            if (liquidity <= 0)
                throw new ArgumentOutOfRangeException(LiqudityException);

            if (holdings == null)
                throw new ArgumentNullException(HoldingsNullException);

            if(!holdings.Any())
                throw new ArgumentNullException(HoldingsEmptyException);

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
                throw new ArgumentNullException("Beginning " + HoldingsNullException);

            if(!beginningHoldings.Any())
                throw new ArgumentNullException("Beginning " + HoldingsEmptyException);

            if (endingHoldings == null)
                throw new ArgumentNullException("Ending " + HoldingsNullException);

            if (!endingHoldings.Any())
                throw new ArgumentNullException("Ending " + HoldingsEmptyException);

            var startingCost = Cost(beginningHoldings, liquidity);
            var endingCost = Cost(endingHoldings, liquidity);
            return endingCost - startingCost;
        }

        public IEnumerable<double> CurrentPrices(IEnumerable<int> holdings, double liquidity)
        {
            if (liquidity <= 0)
                throw new ArgumentOutOfRangeException(LiqudityException);

            if (holdings == null)
                throw new ArgumentNullException(HoldingsNullException);

            if (!holdings.Any())
                throw new ArgumentNullException(HoldingsEmptyException);

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
                throw new ArgumentNullException(HoldingsNullException);

            if (!holdings.Any())
                throw new ArgumentNullException(HoldingsEmptyException);

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
