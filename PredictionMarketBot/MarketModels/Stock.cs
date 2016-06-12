using System.ComponentModel.DataAnnotations.Schema;

namespace PredictionMarketBot.MarketModels
{
    public class Stock
    {
        public int Id { get; set; }
        public int MarketId { get; set; }
        public virtual Market Market { get; set; }
        public string Name { get; set; }
        public int NumberSold { get; set; }

        [NotMapped]
        public double CurrentPrice { get; set; }
        [NotMapped]
        public double CurrentProbability { get; set; }
    }
}
