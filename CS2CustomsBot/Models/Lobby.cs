namespace CS2CustomsBot.Models
{
    public class Lobby
    {
        public ulong ChannelId { get; set; }
        public ulong CreatedByUserId { get; set; }
        public int MaxPlayers { get; set; }
        public ulong? LobbyMessageId { get; set; }
        public LobbyState State { get; set; } = LobbyState.Open;

        public List<LobbyPlayer> Players { get; set; } = new();

        public List<LobbyPlayer> TeamA {  get; set; } = new();

        public List<LobbyPlayer> TeamB { get; set; } = new();

        public string Winner {  get; set; }
    }
}
