using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PredictionMarketBot
{
    public class Bot : IDisposable
    {
        private DiscordSocketClient Client { get; set; }
        private CommandService Commands { get; set; }
        private IServiceProvider Services { get; set; }
        private MarketsManager Manager { get; set; }

        public Bot(string appName, MarketsManager manager)
        {
            Manager = manager;

            var clientConfig = new DiscordSocketConfig
            {
                MessageCacheSize = 0,
                LogLevel = LogSeverity.Info
            };

            Commands = new CommandService();

            Services = new ServiceCollection()
                .AddSingleton(manager)
                .BuildServiceProvider();

            Client = new DiscordSocketClient(clientConfig);

            Client.Log += OnLogMessage;
        }

        public async Task Start(string token)
        {
            await InstallCommandsAsync();

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task InstallCommandsAsync()
        {
            Client.MessageReceived += HandleCommand;
            await Commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            if (message.Author.IsBot)
                return;

            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasStringPrefix(@"$market ", ref argPos) || message.HasMentionPrefix(Client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new CommandContext(Client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await Commands.ExecuteAsync(context, argPos, Services);
            await HandleCommandResult(context, result);
        }
        private async Task HandleCommandResult(CommandContext context, IResult result)
        {
            if (result.IsSuccess)
            {
                await OnLogMessage(new LogMessage(LogSeverity.Info, "Command Result", $"{context.Message} ({context.User.Username})"));
            }
            else
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        private Task OnLogMessage(LogMessage lm)
        {
            //Color
            ConsoleColor color;
            switch (lm.Severity)
            {
                case LogSeverity.Error: color = ConsoleColor.Red; break;
                case LogSeverity.Warning: color = ConsoleColor.Yellow; break;
                case LogSeverity.Info: color = ConsoleColor.White; break;
                case LogSeverity.Verbose: color = ConsoleColor.Gray; break;
                case LogSeverity.Debug: default: color = ConsoleColor.DarkGray; break;
            }

            //Exception
            string exMessage;
            Exception ex = lm.Exception;
            if (ex != null)
            {
                while (ex is AggregateException && ex.InnerException != null)
                    ex = ex.InnerException;
                exMessage = ex.Message;
            }
            else
                exMessage = null;

            //Source
            string sourceName = lm.Source;

            //Text
            string text;
            if (lm.Message == null)
            {
                text = exMessage ?? "";
                exMessage = null;
            }
            else
                text = lm.Message;

            //Build message
            StringBuilder builder = new StringBuilder(text.Length + (sourceName?.Length ?? 0) + (exMessage?.Length ?? 0) + 5);
            if (sourceName != null)
            {
                builder.Append('[');
                builder.Append(sourceName);
                builder.Append("] ");
            }
            for (int i = 0; i < text.Length; i++)
            {
                //Strip control chars
                char c = text[i];
                if (!char.IsControl(c))
                    builder.Append(c);
            }
            if (exMessage != null)
            {
                builder.Append(": ");
                builder.Append(exMessage);
            }

            text = builder.ToString();
            Console.ForegroundColor = color;
            Console.WriteLine(text);

            return Task.CompletedTask;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Client.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
