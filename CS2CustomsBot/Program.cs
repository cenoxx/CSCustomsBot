using CS2CustomsBot.Models;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace CS2CustomsBot
{
    public class Program
    {
        private DiscordSocketClient? _client;
        private CommandService? _commands;
        private InteractionService? _interactions;
        private CommandHandler? _handler;
        private IServiceProvider? _services;

        private string? _environment;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            var appConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            string? token = appConfig["Discord:Token"];
            _environment = appConfig["Environment"];

            var config = new DiscordSocketConfig
            {
                GatewayIntents =
                    GatewayIntents.Guilds |
                    GatewayIntents.GuildMessages |
                    GatewayIntents.MessageContent |
                    GatewayIntents.GuildVoiceStates
            };

            _client = new DiscordSocketClient(config);
            _commands = new CommandService();
            _interactions = new InteractionService(_client.Rest);

            //Direct injection so these are available across the whole app.
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton(_interactions)
                .AddSingleton<LobbyService>()
                .BuildServiceProvider();

            _handler = new CommandHandler(_client, _commands, _services);

            _client.Log += LogAsync;
            _commands.Log += LogAsync;
            _interactions.Log += LogAsync;

            _client.Ready += ReadyAsync;
            _client.InteractionCreated += HandleInteractionAsync;

            await _handler.InstallCommandsAsync();

            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }

        private async Task ReadyAsync()
        {
            Console.WriteLine($"Connected as -> [{_client.CurrentUser}]");

            if(_environment != null)
            {
                if(_environment == "DEVELOPMENT")
                {
                    ulong guildId = 1495616420095070391;
                    await _interactions.RegisterCommandsToGuildAsync(guildId);
                }
                else
                {
                    await _interactions.RegisterCommandsGloballyAsync();
                }
            }

            Console.WriteLine(_environment);
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, interaction);
                await _interactions.ExecuteCommandAsync(context, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        //Log to console.
        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}