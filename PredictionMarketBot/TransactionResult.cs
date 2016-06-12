namespace PredictionMarketBot
{
    public class TransactionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public double Value { get; set; }
        public string Player { get; set; }
        public string Stock { get; set; }
    }
}
