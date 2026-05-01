namespace CS2CustomsBot.Models
{
    public class LobbyService
    {
        private readonly Dictionary<ulong, Lobby> _lobbiesByChannel = new();

        public bool CreateLobby(ulong channelId, ulong createdByUserId, int maxPlayers)
        {
            if (_lobbiesByChannel.ContainsKey(channelId))
            {
                return false;
            }

            _lobbiesByChannel[channelId] = new Lobby
            {
                ChannelId = channelId,
                CreatedByUserId = createdByUserId,
                MaxPlayers = maxPlayers
            };

            return true;
        }

        public Lobby? GetLobby(ulong channelId)
        {
            _lobbiesByChannel.TryGetValue(channelId, out var lobby);
            return lobby;
        }

        public bool RemoveLobby(ulong channelId)
        {
            return _lobbiesByChannel.Remove(channelId);
        }

        public bool JoinLobby(ulong channelId, ulong userID, string user)
        {
            Lobby? lobby = GetLobby(channelId);

            if (lobby != null)
            {
                if(lobby.Players.Count == lobby.MaxPlayers)
                {
                    return false;
                }

                LobbyPlayer? playerToAdd = lobby.Players
                .FirstOrDefault(p => p.DiscordUserId == userID);

                if(playerToAdd == null)
                {
                    lobby.Players.Add(new LobbyPlayer
                    {
                        DiscordUserId = userID,
                        Username = user,
                        IsReady = false
                    });
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool LeaveLobby(ulong channelId, ulong userId, string user)
        {
            Lobby? lobby = GetLobby(channelId);

            if (lobby == null || lobby.Players.Count == 0)
                return false;

            LobbyPlayer? playerToRemove = lobby.Players
                .FirstOrDefault(p => p.DiscordUserId == userId);

            if (playerToRemove == null)
                return false;

            lobby.Players.Remove(playerToRemove);
            return true;
        }

        

        public bool ReadyUp(ulong channelId, ulong userId, string user)
        {
            Lobby? lobby = GetLobby(channelId);

            if(lobby == null || lobby.Players.Count == 0) return false;

            LobbyPlayer? readyPlayer = lobby.Players.FirstOrDefault(p => p.DiscordUserId == userId);

            if(readyPlayer == null) return false;

            if (readyPlayer.IsReady)
            {
                readyPlayer.IsReady = false;
            }
            else
            {
                readyPlayer.IsReady = true;
            }

            ReadyCheck(channelId);

            return true;
        }

        public bool ReadyCheck(ulong channelId)
        {
            Lobby? lobby = GetLobby(channelId);

            if(lobby == null || lobby.Players.Count == 0) return false;

            if(lobby.Players.Count == lobby.MaxPlayers)
            {
                foreach (var player in lobby.Players)
                {
                    if (player.IsReady == false)
                    {
                        //if a player is not ready, return false.
                        return false;
                    }
                }
                lobby.State = LobbyState.ReadyChecked;
                return true;
            }
            return false;
        }

        public bool SetWinner(ulong channelId, string team)
        {
            Lobby? lobby = GetLobby(channelId);

            if(lobby != null && lobby.TeamA.Count > 0 && lobby.TeamB.Count > 0)
            {
                lobby.Winner = team;
                return true;
            }
            return false;
        }
    }
}
