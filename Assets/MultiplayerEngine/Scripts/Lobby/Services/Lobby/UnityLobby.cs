#if UNITY_SERVICES
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Provides Unity lobby management using Unity Services.
    /// </summary>
    public class UnityLobby : ILobby
    {
        public event Action<LobbyData> LobbyCreated;
        public event Action<LobbyData> LobbyJoined;
        public event Action LobbyLeft;
        public event Action<LobbyData> LobbyUpdated;
        public event Action<LobbyData> LobbyPlayerDataUpdated;
        public event Action<LobbyData> GameStarted;

        private Lobby currentLobby;

        /// <inheritdoc />
        public Task<bool> InitializeAsync()
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public async Task<LobbyData> CreateLobbyAsync(
        string lobbyName,
        bool isPrivate,
        int maxPlayers,
        IDictionary<LobbyDataKeys, string> lobbyData,
        IDictionary<PlayerDataKeys, string> playerData)
        {
            try
            {
                // Convert enum keys to string for Unity Lobby API
                var player = new Player
                {
                    Data = playerData?.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, kvp.Value)
                    )
                };

                var options = new CreateLobbyOptions
                {
                    Player = player,
                    IsPrivate = isPrivate,
                    Data = lobbyData?.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => new DataObject(DataObject.VisibilityOptions.Member, kvp.Value)
                    )
                };

                var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
                currentLobby = lobby;

                await SubscribeToLobbyEventsAsync(lobby.Id);

                var lobbyDataResult = MapLobbyToLobbyData(lobby);
                LobbyCreated?.Invoke(lobbyDataResult);
                return lobbyDataResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"CreateLobbyAsync failed: {ex}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LobbyData> JoinLobbyAsync(string lobbyId, IDictionary<PlayerDataKeys, string> playerData)
        {
            try
            {
                // Convert enum keys to string for Unity Lobby API
                var joinOptions = new JoinLobbyByIdOptions
                {
                    Player = new Player
                    {
                        Data = playerData != null
                            ? playerData.ToDictionary(
                                kvp => kvp.Key.ToString(),
                                kvp => new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, kvp.Value))
                            : null
                    }
                };

                var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
                currentLobby = lobby;

                await SubscribeToLobbyEventsAsync(lobby.Id);

                var lobbyDataResult = MapLobbyToLobbyData(lobby);
                LobbyJoined?.Invoke(lobbyDataResult);
                return lobbyDataResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"JoinLobbyAsync failed: {ex}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LobbyData> JoinLobbyByCodeAsync(string joinCode, IDictionary<PlayerDataKeys, string> playerData)
        {
            try
            {
                var joinOptions = new JoinLobbyByCodeOptions
                {
                    Player = new Player
                    {
                        Data = playerData?.ToDictionary(
                            kvp => kvp.Key.ToString(),
                            kvp => new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, kvp.Value)
                        )
                    }
                };

                var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode, joinOptions);
                currentLobby = lobby;

                await SubscribeToLobbyEventsAsync(lobby.Id);

                var lobbyDataResult = MapLobbyToLobbyData(lobby);
                LobbyJoined?.Invoke(lobbyDataResult);
                return lobbyDataResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"JoinLobbyByCodeAsync failed: {ex}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> LeaveLobbyAsync()
        {
            try
            {
                if (currentLobby == null)
                    return false;

                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                currentLobby = null;

                NetworkManager.Singleton.Shutdown();

                LobbyLeft?.Invoke();
                return true;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"LeaveLobbyAsync failed: {e}");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<LobbyData> UpdatePlayerDataAsync(IDictionary<PlayerDataKeys, string> playerData)
        {
            try
            {
                if (currentLobby == null)
                {
                    Debug.LogError("UpdatePlayerDataAsync failed: No current lobby.");
                    throw new InvalidOperationException("No current lobby to update player data.");
                }

                if (playerData == null || playerData.Count == 0)
                {
                    Debug.LogWarning("UpdatePlayerDataAsync called with null or empty playerData.");
                    return MapLobbyToLobbyData(currentLobby);
                }

                // Convert enum keys to string for Unity Lobby API
                var options = new UpdatePlayerOptions
                {
                    Data = playerData.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, kvp.Value)
                    )
                };

                var lobby = await LobbyService.Instance.UpdatePlayerAsync(
                    currentLobby.Id,
                    AuthenticationService.Instance.PlayerId,
                    options
                );
                currentLobby = lobby;

                var lobbyDataResult = MapLobbyToLobbyData(lobby);
                LobbyPlayerDataUpdated?.Invoke(lobbyDataResult);
                return lobbyDataResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UpdatePlayerDataAsync failed: {ex}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LobbyData> UpdateLobbyDataAsync(IDictionary<LobbyDataKeys, string> lobbyData)
        {
            try
            {
                var options = new UpdateLobbyOptions
                {
                    Data = lobbyData.ToDictionary(
                        kvp => kvp.Key.ToString(), // Convert enum to string
                        kvp => new DataObject(DataObject.VisibilityOptions.Member, kvp.Value)
                    )
                };

                var lobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
                currentLobby = lobby;

                var lobbyDataResult = MapLobbyToLobbyData(lobby);
                LobbyUpdated?.Invoke(lobbyDataResult);
                return lobbyDataResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UpdateLobbyDataAsync failed: {ex}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<LobbyListData>> GetLobbyListAsync()
        {
            try
            {
                var queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
                return queryResponse.Results.Select(lobby => new LobbyListData
                {
                    lobbyId = lobby.Id,
                    lobbyName = lobby.Name,
                    currentPlayers = lobby.Players.Count,
                    maxPlayers = lobby.MaxPlayers
                }).ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"GetLobbyListAsync failed: {ex}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LobbyData> UpdateLobbyPrivacyAsync(bool isPrivate)
        {
            try
            {
                var lobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                {
                    IsPrivate = isPrivate
                });
                currentLobby = lobby;

                var lobbyDataResult = MapLobbyToLobbyData(lobby);
                LobbyUpdated?.Invoke(lobbyDataResult);
                return lobbyDataResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UpdateLobbyPrivacy failed: {ex}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<LobbyData> KickPlayerAsync(string playerId)
        {
            try
            {
                if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
                {
                    var lobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                    return MapLobbyToLobbyData(lobby);
                }

                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
                var updatedLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                return MapLobbyToLobbyData(updatedLobby);
            }
            catch (Exception ex)
            {
                Debug.LogError($"KickPlayerAsync failed: {ex}");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task StartGameAsync()
        {
            try
            {
                if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
                {
                    currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                    {
                        IsLocked = true
                    });

                    var allocation = await RelayService.Instance.CreateAllocationAsync(currentLobby.MaxPlayers);
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

                    string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                    var options = new UpdateLobbyOptions
                    {
                        Data = new Dictionary<string, DataObject>
                        {
                            { LobbyDataKeys.GameId.ToString(), new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                        }
                    };

                    var lobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);

                    GameStarted?.Invoke(MapLobbyToLobbyData(currentLobby));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"StartGameAsync failed: {ex}");
                throw;
            }
        }

        private async Task SubscribeToLobbyEventsAsync(string lobbyId)
        {
            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, callbacks);
        }

        private void OnLobbyChanged(ILobbyChanges changes)
        {
            changes.ApplyToLobby(currentLobby);

            var lobbyDataResult = MapLobbyToLobbyData(currentLobby);

            if (changes.Data.Changed || changes.IsPrivate.Changed || changes.HostId.Changed)
            {
                LobbyUpdated?.Invoke(lobbyDataResult);
            }

            if (changes.PlayerData.Changed)
                LobbyPlayerDataUpdated?.Invoke(lobbyDataResult);
        }

        /// <summary>
        /// Maps a Unity Lobby object to the LobbyData abstraction.
        /// </summary>
        private LobbyData MapLobbyToLobbyData(Lobby lobby)
        {
            var result = new LobbyData
            {
                LobbyId = lobby.Id,
                LobbyName = lobby.Name,
                JoinCode = lobby.LobbyCode,
                IsPrivate = lobby.IsPrivate,
                HostId = lobby.HostId,
                // Ensure correct enum key usage for Data dictionary
                Data = lobby.Data != null
                    ? lobby.Data.ToDictionary(
                        kvp => Enum.TryParse<LobbyDataKeys>(kvp.Key, out var key) ? key : LobbyDataKeys.GameMode, // fallback to a default
                        kvp => kvp.Value?.Value ?? string.Empty)
                    : new Dictionary<LobbyDataKeys, string>(),
                Players = lobby.Players != null
                    ? lobby.Players.Select(p => new PlayerData
                    {
                        PlayerId = p.Id,
                        IsLobbyHost = p.Id == lobby.HostId,
                        IsLocalPlayer = p.Id == AuthenticationService.Instance.PlayerId,
                        // Map PlayerName if available
                        PlayerName = p.Data != null && p.Data.TryGetValue(nameof(PlayerDataKeys.PlayerName), out var nameObj)
                            ? nameObj?.Value ?? string.Empty
                            : string.Empty,
                        Data = p.Data != null
                            ? p.Data.ToDictionary(
                                kvp => Enum.TryParse<PlayerDataKeys>(kvp.Key, out var key) ? key : PlayerDataKeys.PlayerID, // fallback to a default
                                kvp => kvp.Value?.Value ?? string.Empty)
                            : new Dictionary<PlayerDataKeys, string>()
                    }).ToList()
                    : new List<PlayerData>()
            };
            return result;
        }
    }
}
#endif