using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine 
{
    /// <summary>
    /// Manages the character selection UI in the multiplayer lobby.
    /// </summary>

public class LobbyCharacterSelection : MonoBehaviour
{
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private Transform characterSpawnPoint;

        private GameObject currentCharacterInstance;
        private List<CharacterData> characterData;
        private CharacterData selectedCharacter;
        private int selectedCharacterIndex = 0;

        public void Initialize()
        {
            nextButton.onClick.AddListener(SelectNextCharacter);
            previousButton.onClick.AddListener(SelectPreviousCharacter);
            LobbyManager.OnLobbyJoined += _ =>  UpdateCharacter();
            LobbyManager.OnLobbyCreated += _ => UpdateCharacter();
            UpdateCharacterDisplay();
        }

        private async void UpdateCharacter()
        {
            UpdateCharacterDisplay();
            if (LobbyManager.Instance != null)
            {
                await LobbyManager.Instance.UpdateCharacter(selectedCharacter.CharacterId);
            }
        }

        private void UpdateCharacterDisplay()
        {
            if(PlayerProfileManager.Instance == null)
                return;

            characterData = PlayerProfileManager.Instance.CharacterData;

            if (characterData != null)
            {
                CharacterData data =  characterData.FirstOrDefault();
                if (data != null)
                {
                    selectedCharacter = data;
                    characterNameText.text = data.CharacterName;

                    if (currentCharacterInstance != null)
                    {
                        Destroy(currentCharacterInstance);
                    }
                    
                    currentCharacterInstance = Instantiate(selectedCharacter.CharacterLobbyPrefab, characterSpawnPoint.position, characterSpawnPoint.rotation);
                }
            }
        }

        private async void SelectPreviousCharacter()
        {
            if (characterData == null || characterData.Count == 0)
                return;

            previousButton.interactable = false;

            selectedCharacterIndex = (selectedCharacterIndex - 1 + characterData.Count) % characterData.Count;

            selectedCharacter = characterData[selectedCharacterIndex];
            characterNameText.text = selectedCharacter.CharacterName;
            if (currentCharacterInstance != null)
            {
                Destroy(currentCharacterInstance);
            }
            currentCharacterInstance = Instantiate(selectedCharacter.CharacterLobbyPrefab, characterSpawnPoint.position, characterSpawnPoint.rotation);

            await LobbyManager.Instance.UpdateCharacter(selectedCharacter.CharacterId);

            previousButton.interactable = true;
        }

        private async void SelectNextCharacter()
        {
            if (characterData == null || characterData.Count == 0)
                return;

            nextButton.interactable = false;

            selectedCharacterIndex = (selectedCharacterIndex + 1) % characterData.Count;

            selectedCharacter = characterData[selectedCharacterIndex];
            characterNameText.text = selectedCharacter.CharacterName;

            if (currentCharacterInstance != null)
            {
                Destroy(currentCharacterInstance);
            }
            currentCharacterInstance = Instantiate(selectedCharacter.CharacterLobbyPrefab, characterSpawnPoint.position, characterSpawnPoint.rotation);
        
            await LobbyManager.Instance.UpdateCharacter(selectedCharacter.CharacterId);

            nextButton.interactable = true;
        }
    }
}
