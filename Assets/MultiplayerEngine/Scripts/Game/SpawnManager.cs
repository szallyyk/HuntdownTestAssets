#if UNITY_SERVICES || STEAM_SERVICES
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    public class SpawnManager : NetworkBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        private Dictionary<ulong, NetworkObject> spawnedCharacters = new Dictionary<ulong, NetworkObject>();

        private int spawnIndex = 0;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(SessionManager.Instance.SelectedCharacter == null)
            {
                //can use inagame character selection system to set this value
                Debug.LogError("No character selected for spawning. Please select a character before starting the game.");
                return;
            }

            SessionManager.OnAllPlayersLoaded += () =>
            {
                SpawnCharacterForClientRpc(NetworkManager.Singleton.LocalClientId, SessionManager.Instance.SelectedCharacter.CharacterId);
                NetworkManager.OnClientDisconnectCallback += DespawnCharacterForClient;
            };
        }

        [Rpc(SendTo.Server)]
        public void SpawnCharacterForClientRpc(ulong clientId, FixedString32Bytes CharacterID)
        {
            if (!IsServer) return;

            Transform spawnPoint = (spawnPoints.Length > 0)
                ? spawnPoints[spawnIndex]
                : null;

            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            CharacterData characterData = PlayerProfileManager.Instance.CharacterData.First(CharacterData => CharacterData.CharacterId == CharacterID);

            GameObject newCharacter = Instantiate(characterData.CharacterPrefab, spawnPosition, spawnRotation);
            NetworkObject networkObject = newCharacter.GetComponent<NetworkObject>();

            networkObject.SpawnAsPlayerObject(clientId);
            spawnedCharacters[clientId] = networkObject;

            // Move to the next spawn point, loop back if at the end
            spawnIndex = (spawnIndex + 1) % spawnPoints.Length;
        }

        private void DespawnCharacterForClient(ulong clientId)
        {
            if (IsServer && spawnedCharacters.TryGetValue(clientId, out NetworkObject character))
            {
                character.Despawn();
                spawnedCharacters.Remove(clientId);
            }
        }
    }
}
#endif
