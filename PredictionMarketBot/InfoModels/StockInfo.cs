using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictionMarketBot.InfoModels
{
    public class StockInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int NumberSold { get; set; }
        public double CurrentPrice { get; set; }
        public double CurrentProbability { get; set; }
    }
}
