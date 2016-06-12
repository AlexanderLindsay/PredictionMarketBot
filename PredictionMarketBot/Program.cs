using Nito.AsyncEx;
using PredictionMarketBot.MarketModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictionMarketBot
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }

        static async void MainAsync(string[] args)
        {
            using(var context = new MarketContext())
            {
                var market = context.Markets.FirstOrDefault();
                if (market == null)
                {
                    market = new Market() { Liquidity = 100.0, IsRunning = false };
                    context.Markets.Add(market);
                    context.SaveChanges();
                    context.Entry(market).Reload();
                }

                var simulator = new MarketSimulator(context, market.Id);

                var loop = true;

                while(loop)
                {
                    Console.Write("> ");
                    var line = Console.ReadLine();
                    var parts = line.Split(' ');

                    if(parts.Length > 0)
                    {
                        loop = await RunCommand(parts, simulator);
                    }
                }
            }
        }

        static async Task<bool> RunCommand(string[] command, MarketSimulator simulator)
        {
            switch (command[0].ToLowerInvariant())
            {
                case "exit":
                    return false;
                case "list":
                    var target = "all";
                    if(command.Length >= 2)
                    {
                        target = command[1].ToLowerInvariant();
                    }
                    List(simulator, target);
                    break;
                case "add":
                    if (command.Length < 2)
                    {
                        Console.WriteLine("Add requires additional parameters");
                        return false;
                    }
                    await Add(simulator, command.Skip(1));
                    break;
                case "start":
                    await simulator.Start();
                    break;
                case "stop":
                    await simulator.Stop();
                    break;
                case "buy":
                    await Buy(simulator, command.Skip(1));
                    break;
                case "sell":
                    await Sell(simulator, command.Skip(1));
                    break;
                case "clear":
                    Console.Clear();
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;                
            }

            return true;
        }

        static void List(MarketSimulator simulator, string target)
        {
            switch (target)
            {
                case "stocks":
                    Console.WriteLine("Stocks");

                    var stocks = simulator.ListStocks();
                    foreach(var stock in stocks)
                    {
                        Console.WriteLine("Stock {0} ({1}):", stock.Name, stock.Id);
                        Console.WriteLine("Shares Owned: {0}", stock.NumberSold);
                        Console.WriteLine("Current Price: {0:C}", stock.CurrentPrice);
                        Console.WriteLine("Probability: {0:F4}", stock.CurrentProbability);
                        Console.WriteLine("-----------------");
                    }

                    Console.WriteLine("End Stocks");
                    break;
                case "players":
                    Console.WriteLine("Players");

                    var players = simulator.ListPlayers();
                    foreach(var player in players)
                    {
                        Console.WriteLine("Player {0}", player.Name);
                        Console.WriteLine("Funds: {0:C}", player.Money);
                        Console.WriteLine("Shares: {0}", player.Shares.Aggregate("",
                            (str, share) => str += string.Format("{0}:{1}|", share.Stock.Name, share.Amount)));
                    }

                    Console.WriteLine("End Players");
                    break;
                case "all":
                    List(simulator, "stocks");
                    List(simulator, "players");
                    break;
                default:
                    Console.WriteLine("Unknown list target.");
                    break;
            }
        }

        static async Task Add(MarketSimulator simulator, IEnumerable<string> arguments)
        {
            var target = arguments.First();
            switch (target)
            {
                case "player":
                    await simulator.AddPlayerAsync(new Player()
                    {
                        Name = "TestPlayer",
                        Money = 500.0
                    });
                    break;
                case "stock":
                    await simulator.AddStockAsync(new Stock()
                    {
                        Name = "TestStock",
                        NumberSold = 0
                    });
                    break;
                default:
                    Console.WriteLine("Invalid target for add.");
                    break;
            }
        }

        static async Task Buy(MarketSimulator simulator, IEnumerable<string> arguments)
        {

            if (arguments.Count() < 3)
            {
                Console.WriteLine("Invalid arguments for buy");
                return;
            }

            int playerId;
            int stockId;
            int amount;

            if(
                !int.TryParse(arguments.ElementAt(0), out playerId) ||
                !int.TryParse(arguments.ElementAt(1), out stockId) ||
                !int.TryParse(arguments.ElementAt(2), out amount))
            {
                Console.WriteLine("Invalid arguments for buy");
                return;
            }

            var result = await simulator.Buy(playerId, stockId, amount);
            if (result.Success)
            {
                Console.WriteLine("Player {0} bought {1} {2} of stock {3} for {4:C}", 
                    result.Player, amount, amount == 1 ? "share" : "shares", result.Stock, result.Value);
            }else
            {
                Console.WriteLine("Player {0} failed to buy {1} {2} of stock {3} because: {4}",
                    result.Player, amount, amount == 1 ? "share" : "shares", result.Stock, result.Message);
            }
        }

        static async Task Sell(MarketSimulator simulator, IEnumerable<string> arguments)
        {

            if (arguments.Count() < 3)
            {
                Console.WriteLine("Invalid arguments for sell");
                return;
            }

            int playerId;
            int stockId;
            int amount;

            if (
                !int.TryParse(arguments.ElementAt(0), out playerId) ||
                !int.TryParse(arguments.ElementAt(1), out stockId) ||
                !int.TryParse(arguments.ElementAt(2), out amount))
            {
                Console.WriteLine("Invalid arguments for sell");
                return;
            }

            var result = await simulator.Sell(playerId, stockId, amount);
            if (result.Success)
            {
                Console.WriteLine("Player {0} sold {1} {2} of stock {3} for {4:C}",
                    result.Player, amount, amount == 1 ? "share" : "shares", result.Stock, -1 * result.Value);
            }
            else
            {
                Console.WriteLine("Player {0} failed to sell {1} {2} of stock {3} because: {4}",
                    result.Player, amount, amount == 1 ? "share" : "shares", result.Stock, result.Message);
            }
        }
    }
}
