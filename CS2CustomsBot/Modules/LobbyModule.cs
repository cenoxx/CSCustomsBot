using CS2CustomsBot.Models;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace CS2CustomsBot.Modules
{
    public class LobbyModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly LobbyService _lobbyService;

        public LobbyModule(LobbyService lobbyService)
        {
            _lobbyService = lobbyService;
        }

        // =========================================================
        // Slash Commands
        // =========================================================

        [SlashCommand("createlobby", "Create a lobby")]
        public async Task CreateLobbyAsync(int maxPlayers)
        {
            bool created = _lobbyService.CreateLobby(
                Context.Channel.Id,
                Context.User.Id,
                maxPlayers);

            if (!created)
            {
                await RespondAsync("A lobby already exists in this channel.", ephemeral: true);
                return;
            }

            var lobby = _lobbyService.GetLobby(Context.Channel.Id);

            await RespondAsync(
                FormatLobby(lobby),
                components: BuildLobbyButtons(lobby));

            var message = await GetOriginalResponseAsync();

            if (lobby != null)
            {
                lobby.LobbyMessageId = message.Id;
            }
        }

        [SlashCommand("cancellobby", "Cancel the current lobby")]
        public async Task CancelLobbyAsync()
        {
            var lobby = _lobbyService.GetLobby(Context.Channel.Id);

            if (lobby == null)
            {
                await RespondAsync("There is no active lobby in this channel.", ephemeral: true);
                return;
            }

            if (lobby.CreatedByUserId != Context.User.Id)
            {
                await RespondAsync("Only the lobby creator can cancel this lobby.", ephemeral: true);
                return;
            }

            await MarkLobbyCancelledAsync(lobby);
            _lobbyService.RemoveLobby(Context.Channel.Id);

            await RespondAsync("Lobby cancelled.", ephemeral: true);
        }

        // =========================================================
        // Component Interactions
        // =========================================================

        [ComponentInteraction("lobby:join:*")]
        public async Task JoinLobbyAsync(string channelId)
        {
            ulong parsedChannelId = ParseChannelId(channelId);
            string username = GetDisplayName();

            _lobbyService.JoinLobby(parsedChannelId, Context.User.Id, username);

            await RefreshLobbyMessageAsync(parsedChannelId);
        }

        [ComponentInteraction("lobby:leave:*")]
        public async Task LeaveLobbyAsync(string channelId)
        {
            ulong parsedChannelId = ParseChannelId(channelId);
            string username = GetDisplayName();

            _lobbyService.LeaveLobby(parsedChannelId, Context.User.Id, username);

            await RefreshLobbyMessageAsync(parsedChannelId);
        }

        [ComponentInteraction("lobby:ready:*")]
        public async Task ReadyUpAsync(string channelId)
        {
            ulong parsedChannelId = ParseChannelId(channelId);
            string username = GetDisplayName();

            _lobbyService.ReadyUp(parsedChannelId, Context.User.Id, username);

            await RefreshLobbyMessageAsync(parsedChannelId);
        }

        [ComponentInteraction("lobby:start:*")]
        public async Task StartGameAsync(string channelId)
        {
            ulong parsedChannelId = ParseChannelId(channelId);
            var lobby = _lobbyService.GetLobby(parsedChannelId);

            if (lobby != null)
            {
                lobby.State = LobbyState.InProgess;
            }
            else
            {
                return;
            }

            if(lobby.TeamA.Count == 0 && lobby.TeamB.Count == 0)
                await RefreshLobbyMessageAsync(parsedChannelId);

            string teamA = "**Team A**\n";
            string teamB = "**Team B**\n";

            foreach(var player in lobby.TeamA)
            {
                teamA += player.Username + "\n";
            }

            foreach(var player in lobby.TeamB)
            {
                teamB += player.Username + "\n";
            }

            string content = $"**Lobby**\n\n{teamA}\n{teamB}\n**Game Started! GLHF**";
            await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
            {
                msg.Content = content;
                msg.Components = BuildLobbyButtons(lobby);
            });
        }

        [ComponentInteraction("lobby:end:*")]
        public async Task EndGameAsync(string channelId)
        {
            ulong parsedChannelId = ParseChannelId(channelId);
            var lobby = _lobbyService.GetLobby(parsedChannelId);

            if(lobby != null)
            {
                lobby.State = LobbyState.Finished;
            }

            await RefreshLobbyMessageAsync(parsedChannelId);
        }

        [ComponentInteraction("lobby:recordTeamTrue:*")]
        public async Task RecordTeamTrueAsync(string channelId)
        {
            ulong parsedChannelId = ParseChannelId(channelId);
            var lobby = _lobbyService.GetLobby(parsedChannelId);

            if(lobby != null && lobby.State == LobbyState.Finished)
            {
                lobby.State = LobbyState.RecordTeams;
            }

            await RefreshLobbyMessageAsync(parsedChannelId);
        }

        [ComponentInteraction("lobby:recordTeamFalse:*")]
        public async Task RecordTeamFalseAsync(string channelId)
        {
            ulong parsedChannelId = ParseChannelId(channelId);
            var lobby = _lobbyService.GetLobby(parsedChannelId);

            if(lobby != null)
            {
                _lobbyService.RemoveLobby(parsedChannelId);
                await MarkLobbyFinishedAsync(lobby, false, parsedChannelId);
            }
            
        }

        [ComponentInteraction("lobby:recordTeamA:*")]
        public async Task RecordTeamAAsync(string channelId)
        {
            ulong parsedChannelId = ParseChannelId(channelId);
            var lobby = _lobbyService.GetLobby(parsedChannelId);

            if (lobby != null)
            {
                _lobbyService.SetWinner(parsedChannelId, "TeamA");
                await MarkLobbyFinishedAsync(lobby, true, parsedChannelId);
            }

        }

        [ComponentInteraction("lobby:recordTeamB:*")]
        public async Task RecordTeamBAsync(string channelId)
        {
            ulong parsedChannelId = ParseChannelId(channelId);
            var lobby = _lobbyService.GetLobby(parsedChannelId);

            if (lobby != null)
            {
                _lobbyService.SetWinner(parsedChannelId, "TeamB");
                await MarkLobbyFinishedAsync(lobby, true, parsedChannelId);
            }

        }

        [ComponentInteraction("lobby:randomize:*")]
        public async Task RandomizeTeamsAsync(string channelId)
        {
            ulong parsedChannelId = ParseChannelId(channelId);
            var lobby = _lobbyService.GetLobby(parsedChannelId);

            if (lobby == null)
            {
                await RespondAsync("Lobby not found.", ephemeral: true);
                return;
            }

            var shuffledPlayers = lobby.Players
                .OrderBy(_ => Random.Shared.Next())
                .ToList();

            int teamASize = (shuffledPlayers.Count + 1) / 2;

            var teamA = shuffledPlayers.Take(teamASize).ToList();
            var teamB = shuffledPlayers.Skip(teamASize).ToList();

            string teamAText = string.Join("\n", teamA.Select(p => p.Username));
            string teamBText = string.Join("\n", teamB.Select(p => p.Username));

            lobby.TeamA = teamA;
            lobby.TeamB = teamB;

            string content =
        $@"**Teams Randomized**

**Team A**
{teamAText}

**Team B**
{teamBText}";

            await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
            {
                msg.Content = content;
                msg.Components = BuildLobbyButtons(lobby);
            });
        }

        // =========================================================
        // UI / Rendering
        // =========================================================

        private static string FormatLobby(Lobby? lobby)
        {
            if (lobby == null)
                return "Lobby not found.";

            string stateText = lobby.State switch
            {
                LobbyState.ReadyChecked => "Lobby is ready to start.",
                LobbyState.InProgess => "Game started, GLHF!",
                LobbyState.Finished => "Game finished! Do you want to record who won?",
                LobbyState.RecordTeams => "Which team won? Team A or Team B?",
                _ => string.Empty
            };

            string playerLines = lobby.Players.Count == 0
                ? "No players yet."
                : string.Join("\n", lobby.Players.Select(p =>
                    $"{(p.IsReady ? "✅" : "⏳")} {p.Username}"));

            return
$@"**Lobby**

Players: {lobby.Players.Count}/{lobby.MaxPlayers}

{playerLines}
{stateText}";
        }

        private static MessageComponent BuildLobbyButtons(Lobby? lobby)
        {
            if (lobby == null)
            {
                return new ComponentBuilder().Build();
            }

            var builder = new ComponentBuilder();

            if (lobby.State == LobbyState.InProgess)
            {
                builder
                    .WithButton("Start Game", $"lobby:start:{lobby.ChannelId}", ButtonStyle.Success, disabled: true)
                    .WithButton("Randomize Teams", $"lobby:randomize:{lobby.ChannelId}", ButtonStyle.Secondary, disabled: true)
                    .WithButton("End Game", $"lobby:end:{lobby.ChannelId}", ButtonStyle.Danger);

                return builder.Build();
            }

            if (lobby.State == LobbyState.ReadyChecked)
            {
                builder
                    .WithButton("Start Game", $"lobby:start:{lobby.ChannelId}", ButtonStyle.Success)
                    .WithButton("Randomize Teams", $"lobby:randomize:{lobby.ChannelId}", ButtonStyle.Secondary)
                    .WithButton("End Game", $"lobby:end:{lobby.ChannelId}", ButtonStyle.Danger);

                return builder.Build();
            }

            if(lobby.State == LobbyState.Finished)
            {
                builder
                    .WithButton("Yes", $"lobby:recordTeamTrue:{lobby.ChannelId}", ButtonStyle.Success)
                    .WithButton("No", $"lobby:recordTeamFalse:{lobby.ChannelId}", ButtonStyle.Danger);

                return builder.Build();
            }

            if (lobby.State == LobbyState.RecordTeams)
            {
                builder
                    .WithButton("Team A", $"lobby:recordTeamA:{lobby.ChannelId}", ButtonStyle.Primary)
                    .WithButton("Team B", $"lobby:recordTeamB:{lobby.ChannelId}", ButtonStyle.Secondary);

                return builder.Build();
            }

            builder
                .WithButton("Join", $"lobby:join:{lobby.ChannelId}", ButtonStyle.Success)
                .WithButton("Ready Up", $"lobby:ready:{lobby.ChannelId}", ButtonStyle.Secondary)
                .WithButton("Leave", $"lobby:leave:{lobby.ChannelId}", ButtonStyle.Danger);

            return builder.Build();
        }

        private static MessageComponent BuildCancelledButtons()
        {
            return new ComponentBuilder()
                .WithButton("Join", "lobby:disabled:join", ButtonStyle.Success, disabled: true)
                .WithButton("Ready Up", "lobby:disabled:ready", ButtonStyle.Secondary, disabled: true)
                .WithButton("Leave", "lobby:disabled:leave", ButtonStyle.Danger, disabled: true)
                .Build();
        }

        // =========================================================
        // Helpers
        // =========================================================

        private async Task RefreshLobbyMessageAsync(ulong channelId)
        {
            var lobby = _lobbyService.GetLobby(channelId);

            await (Context.Interaction as SocketMessageComponent)!.UpdateAsync(msg =>
            {
                msg.Content = FormatLobby(lobby);
                msg.Components = BuildLobbyButtons(lobby);
            });
        }

        private async Task MarkLobbyCancelledAsync(Lobby lobby)
        {
            if (Context.Channel is not IMessageChannel channel)
                return;

            var rawMessage = await channel.GetMessageAsync((ulong)lobby.LobbyMessageId);

            if (rawMessage is not IUserMessage lobbyMessage)
                return;

            await lobbyMessage.ModifyAsync(msg =>
            {
                msg.Content = $"**Lobby Cancelled** by {Context.User.Username}";
                msg.Components = BuildCancelledButtons();
            });
        }

        private async Task MarkLobbyFinishedAsync(Lobby lobby, bool recordTeams, ulong channelId)
        {
            if (Context.Channel is not IMessageChannel channel)
                return;

            var rawMessage = await channel.GetMessageAsync((ulong)lobby.LobbyMessageId);

            if (rawMessage is not IUserMessage lobbyMessage)
                return;

            string winner = lobby.Winner;

            var builder = new ComponentBuilder();

            if (!recordTeams)
            {
                string players = "";

                foreach (var player in lobby.Players)
                {
                    players += player.Username + "\n";
                }

                _lobbyService.RemoveLobby(channelId);

                await lobbyMessage.ModifyAsync(msg =>
                {
                    msg.Content = $"**Game Finished** at {DateTime.Now.ToString()} \n\nParticipating Players: \n{players}";
                    msg.Components = null;
                });
            }
            else
            {
                string teamA = "";
                string teamB = "";

                string content = "";

                foreach(var player in lobby.TeamA)
                {
                    teamA += player.Username + "\n";
                }

                foreach(var player in lobby.TeamB)
                {
                    teamB += player.Username + "\n";
                }

                switch(lobby.Winner)
                {
                    case "TeamA":
                        content = $"**Game Finished** at {DateTime.Now.ToString()} \n\n**TEAM A WINS!**\n\n**Team A:**\n{teamA}\nTeam B:\n{teamB}";
                        break;
                    case "TeamB":
                        content = $"**Game Finished** at {DateTime.Now.ToString()} \n\n**TEAM B WINS!**\n\n**Team B:**\n{teamB}\nTeam A\n{teamA}";
                        break;
                }

                _lobbyService.RemoveLobby(channelId);

                await lobbyMessage.ModifyAsync(msg =>
                {
                    msg.Content = content;
                    msg.Components = null;
                });
            }
        }

        private ulong ParseChannelId(string channelId)
        {
            return ulong.Parse(channelId);
        }

        private string GetDisplayName()
        {
            return (Context.User as SocketGuildUser)?.DisplayName ?? Context.User.Username;
        }
    }
}