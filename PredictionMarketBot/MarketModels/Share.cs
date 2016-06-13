using System.ComponentModel.DataAnnotations.Schema;

namespace PredictionMarketBot.MarketModels
{
    public class Share
    {
        public int Id { get; set; }
        public int StockId { get; set; }
        public virtual Stock Stock { get; set; }
        public int PlayerId { get; set; }
        public virtual Player Player { get; set; }
        public int Amount { get; set; }
    }
}
