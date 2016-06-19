using NUnit.Framework;
using PredictionMarketBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictionMarketBot.Tests
{
    [TestFixture()]
    public class LogarithmicMarketScoringRuleTests
    {
        private LogarithmicMarketScoringRule Rule;

        [SetUp]
        public void SetUp()
        {
            Rule = new LogarithmicMarketScoringRule();
        }

        [Test()]
        public void CostTest()
        {
            Assert.Throws<ArgumentNullException>(() => { Rule.Cost(null, 100.0); });
            Assert.Throws<ArgumentNullException>(() => { Rule.Cost(Enumerable.Empty<int>(), 100.0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Rule.Cost(new List<int> { 20, 10 }, -100.0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Rule.Cost(new List<int> { 20, 10 }, 0); });

            //http://www.wolframalpha.com/input/?i=ln(e%5E(20%2F100)+%2B+e%5E(10%2F100))*100
            var holdings = new List<int> { 20, 10 };
            var result = Rule.Cost(holdings, 100.0);
            Assert.That(84.44, Is.EqualTo(result).Within(0.001));

            //http://www.wolframalpha.com/input/?i=ln(e%5E(20%2F100)+%2B+e%5E(10%2F100)+%2B+e%5E(0%2F100))+*+100
            var holdings1 = new List<int> { 20, 10, 0 };
            var result1 = Rule.Cost(holdings1, 100.0);
            Assert.That(120.19, Is.EqualTo(result1).Within(0.005));

            //http://www.wolframalpha.com/input/?i=ln(e%5E(200%2F100)+%2B+e%5E(50%2F100)+%2B+e%5E(300%2F100)+%2B+e%5E(75%2F100)+%2B+e%5E(90%2F100))+*+100
            var holdings2 = new List<int> { 200, 50, 300, 75, 90 };
            var result2 = Rule.Cost(holdings2, 100.0);
            Assert.That(351.75, Is.EqualTo(result2).Within(0.001));

            //http://www.wolframalpha.com/input/?i=ln(e%5E(20%2F50)+%2B+e%5E(10%2F50))*50
            var holdings3 = new List<int> { 20, 10 };
            var result3 = Rule.Cost(holdings3, 50.0);
            Assert.That(49.90, Is.EqualTo(result3).Within(0.01));

            //http://www.wolframalpha.com/input/?i=ln(e%5E(20%2F200)+%2B+e%5E(10%2F200))*200
            var holdings4 = new List<int> { 20, 10 };
            var result4 = Rule.Cost(holdings4, 200.0);
            Assert.That(153.69, Is.EqualTo(result4).Within(0.01));
        }

        [Test()]
        public void CalculateChangeTest()
        {
            var validList = new List<int> { 10, 20 };
            Assert.Throws<ArgumentNullException>(() => { Rule.CalculateChange(null, validList, 100.0); });
            Assert.Throws<ArgumentNullException>(() => { Rule.CalculateChange(Enumerable.Empty<int>(), validList, 100.0); });
            Assert.Throws<ArgumentNullException>(() => { Rule.CalculateChange(validList, null, 100.0); });
            Assert.Throws<ArgumentNullException>(() => { Rule.CalculateChange(validList, Enumerable.Empty<int>(), 100.0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Rule.CalculateChange(validList, validList, -100.0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Rule.CalculateChange(validList, validList, 0); });

            //http://www.wolframalpha.com/input/?i=ln(e%5E(20%2F100)+%2B+e%5E(10%2F100))*100+-+ln(e%5E(0%2F100)+%2B+e%5E(0%2F100))*100
            var startingHoldings = new List<int> { 0, 0 };
            var endingHoldings = new List<int> { 20, 10 };
            var result = Rule.CalculateChange(startingHoldings, endingHoldings, 100.0);
            Assert.That(15.12, Is.EqualTo(result).Within(0.01));

            var startingHoldings1 = new List<int> { 0, 0 };
            var endingHoldings1 = new List<int> { 0, 0 };
            var result1 = Rule.CalculateChange(startingHoldings1, endingHoldings1, 100.0);
            Assert.That(0, Is.EqualTo(result1).Within(0.01));
        }

        [Test()]
        public void CurrentPricesTest()
        {
            Assert.Throws<ArgumentNullException>(() => { Rule.CurrentPrices(null, 100.0); });
            Assert.Throws<ArgumentNullException>(() => { Rule.CurrentPrices(Enumerable.Empty<int>(), 100.0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Rule.CurrentPrices(new List<int> { 20, 10 }, -100.0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Rule.CurrentPrices(new List<int> { 20, 10 }, 0); });

            //http://www.wolframalpha.com/input/?i=ln(e%5E(1%2F100)+%2B+e%5E(0%2F100))+*+100+-+ln(e%5E(0%2F100)+%2B+e%5E(0%2F100))+*+100
            var holdings = new List<int> { 0, 0 };
            var results = Rule.CurrentPrices(holdings, 100.0);

            Assert.AreEqual(2, results.Count());

            foreach (var result in results)
            {
                Assert.That(0.50, Is.EqualTo(result).Within(0.01));
            }

            //http://www.wolframalpha.com/input/?i=ln(e%5E(501%2F100)+%2B+e%5E(0%2F100))+*+100+-+ln(e%5E(500%2F100)+%2B+e%5E(0%2F100))+*+100
            //http://www.wolframalpha.com/input/?i=ln(e%5E(500%2F100)+%2B+e%5E(1%2F100))+*+100+-+ln(e%5E(500%2F100)+%2B+e%5E(0%2F100))+*+100
            var holdings1 = new List<int> { 500, 0 };
            var results1 = Rule.CurrentPrices(holdings1, 100.0).ToArray();
            Assert.That(0.99, Is.EqualTo(results1[0]).Within(0.01));
            Assert.That(0.01, Is.EqualTo(results1[1]).Within(0.01));

            //http://www.wolframalpha.com/input/?i=ln(e%5E(501%2F100)+%2B+e%5E(0%2F100))+*+100+-+ln(e%5E(500%2F100)+%2B+e%5E(0%2F100))+*+100
            //http://www.wolframalpha.com/input/?i=ln(e%5E(500%2F100)+%2B+e%5E(1%2F100))+*+100+-+ln(e%5E(500%2F100)+%2B+e%5E(0%2F100))+*+100
            var holdings2 = new List<int> { 0, 500 };
            var results2 = Rule.CurrentPrices(holdings2, 100.0).ToArray();
            Assert.That(0.01, Is.EqualTo(results2[0]).Within(0.01));
            Assert.That(0.99, Is.EqualTo(results2[1]).Within(0.01));
        }

        [Test()]
        public void ProbabilitiesTest()
        {
            Assert.Throws<ArgumentNullException>(() => { Rule.Probabilities(null, 100.0); });
            Assert.Throws<ArgumentNullException>(() => { Rule.Probabilities(Enumerable.Empty<int>(), 100.0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Rule.Probabilities(new List<int> { 20, 10 }, -100.0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Rule.Probabilities(new List<int> { 20, 10 }, 0); });

            //http://www.wolframalpha.com/input/?i=e%5E(20%2F100)%2F(e%5E(20%2F100)+%2B+e%5E(10%2F100))
            //http://www.wolframalpha.com/input/?i=e%5E(10%2F100)%2F(e%5E(20%2F100)+%2B+e%5E(10%2F100))
            var holdings = new List<int> { 20, 10 };
            var results = Rule.Probabilities(holdings, 100.0).ToArray();
            Assert.That(.52, Is.EqualTo(results[0]).Within(0.01));
            Assert.That(.47, Is.EqualTo(results[1]).Within(0.01));
        }
    }
}