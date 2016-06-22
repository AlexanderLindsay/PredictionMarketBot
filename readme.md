# [Prediction Market Bot](https://github.com/AlexanderLindsay/PredictionMarketBot)

by Alexander Lindsay

With [Prediction Market](https://en.wikipedia.org/wiki/Prediction_market) Bot you can add some meta gaming to your [Discord](https://discordapp.com/) server. Users of your server use points to buy and sell "stock" representing various potential eventualities.  The bot tracks the sales and uses the information to offer a prediction on which eventuality the users think is more likely.

You are free to copy, modify, and distribute <PROJECT NAME> with attribution under the terms of the MIT license. See the LICENSE file for details.

## How to create your own bot

This bot is built in C# and uses the [Discord.Net](https://github.com/RogueException/Discord.Net) to interface with Discord. It will require Windows to run.

#### Create a discord bot
1. Navigate to your discord application page [here](https://discordapp.com/developers/applications/me).
2. Add a new application
3. Add an app bot user
4. Take the client id of your newly created bot and put it in this url: `https://discordapp.com/oauth2/authorize?client_id=clientidhere&scope=bot&permissions=0`
5. Add the bot to a server you own

#### Run Prediction Market Bot
1. Clone or download the source code
2. Add a secrets.config file to the PredictionMarketBot folder that contains the token from your bot user

```xml
<?xml version="1.0" encoding="utf-8" ?>
<appSettings>
  <add key="token" value="YourTokenHere"/>
</appSettings>
```

3. Compile the program and run it (Visual Studio will make this easy and the [community edition](https://www.visualstudio.com/products/visual-studio-community-vs) is free)
4. The bot should have now join your server!

#### Interacting with PredictionMarketBot

Interacting with the bot is easy, just preface your command with $market. Alternativily, `@` the bot. If the bot was called `MarketBot` you could either type `$market list` or `@MarketBot list`, which ever you find easier.

The first command to try is `$market help`. This will list all the other commands that are available. The `help` command can also be used to get furthur information on the other commands. For example, `$market help info` would provide help information on the `info` command.

As the server owner, you will need to set up the markets for your users. Use `$market create market MarketName 100.0 description` to create a new market and follow with `$market switch market MarketName` to set up the active market. Replace the `MarketName` with the name of your market and `description` with a explanation of the market.
After creating the new market add some stocks with `$market add stock StockName`. Once you are satisfied, use `$market open` to allow users to buy and sell stocks.

Once the market is open, users can use `$market buy 10 StockName` and `$market sell 10 StockName` to buy and sell stocks. `$market predict` will offer a prediction of the winner and `$market list` will the current player and stock stats. If a user does not want to participate then they just don't have to buy anything and they won't be included in the game.
