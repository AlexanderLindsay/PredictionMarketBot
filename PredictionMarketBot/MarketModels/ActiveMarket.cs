using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictionMarketBot.MarketModels
{
    public class ActiveMarket
    {
        public int Id { get; set; }
        public string ServerId { get; set; }
        public int MarketId { get; set; }
        public virtual Market Market { get; set; }
    }
}
