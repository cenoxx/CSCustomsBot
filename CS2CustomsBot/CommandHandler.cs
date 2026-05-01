using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;

namespace CS2CustomsBot
{
    
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        private readonly IServiceProvider _services;

        //Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        public async Task InstallCommandsAsync()
        {
            //hook the MessageRecieved event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                services: _services);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            //don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if(message == null)
            {
                return;
            }

            //create a number to track where the prefix ends and the command begins
            int argPos = 0;

            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            //create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            //execute the command with the command context we just
            //created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _services);
        }
    }
}
