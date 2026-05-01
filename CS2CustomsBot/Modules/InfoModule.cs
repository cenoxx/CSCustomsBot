using Discord.Commands;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;

namespace CS2CustomsBot.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task Ping()
        {
            await ReplyAsync("Pong");
        }

        [Command("whois")]
        public async Task WhoAmI()
        {
            var username = Context.User.Username;
            var displayName = (Context.User as SocketGuildUser)?.DisplayName;
            var id = Context.User.Id;

            await ReplyAsync($"Username: {username}\nDisplay: {displayName}\nID: {id}");
        }
    }
}
