namespace CS2CustomsBot.Models
{
    public class LobbyPlayer
    {
        public ulong DiscordUserId { get; set; }
        public string Username { get; set; } = "";
        public bool IsReady { get; set; }
        public int? TeamNumber { get; set; }
    }
}
