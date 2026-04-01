using Discord;
using Discord.WebSocket;
using Microsoft.VisualBasic;

namespace CS2CustomsBot
{
    public class Program
    {
        private static DiscordSocketClient? _client;
        public static async Task Main()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;

            var token = "MTQ4ODg5MzM4MDU2OTA3NTgyNQ.GTZjKm.Kv7B3DEp4VWrzeAC32KSGp75brsKQra41nhIa8";

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();


            await Task.Delay(-1);
        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
