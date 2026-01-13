#if UNITY_SERVICES || STEAM_SERVICES
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ignitives.MultiplayerEngine
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactRadius = 2.0f;
        [SerializeField] private float interactAngle = 60f;
        [SerializeField] private float interactCheckInterval = 0.1f;
        
        private const int MaxOverlapResults = 16;
        
        private NetworkVariable<bool> isObjectPickedUp = new NetworkVariable<bool>();
        private NetworkObject pickedUpObject;
        private InputManager inputManager;
        private PlayerInput playerInput;
        private ThirdPersonController thirdPersonController;
        private PlayerStatsUI playerStatsUI;

        private Interactable lastHighlightedInteractable = null;
        private Interactable currentClosestInteractable = null;
        private Collider[] overlapBuffer = new Collider[MaxOverlapResults];
        private float nextInteractCheckTime;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            thirdPersonController = GetComponent<ThirdPersonController>();
            playerInput = GetComponent<PlayerInput>();
            inputManager = GetComponent<InputManager>();
            playerStatsUI = GetComponentInChildren<PlayerStatsUI>();

            if (!IsOwner) return;

            if (thirdPersonController != null && inputManager != null)
            {
                playerInput.enabled = true;
                inputManager.enabled = true;
                thirdPersonController.enabled = true;
            }

            if(PlayerProfileManager.Instance != null)
            {
                string playerName = PlayerProfileManager.Instance.LocalPlayerStats.DisplayName;
                SetPlayerDataRpc(playerName);
            }
        }

        [Rpc(SendTo.Everyone)]
        public void SetPlayerDataRpc(string playerName)
        {
            if(playerStatsUI!= null)
                playerStatsUI.SetPlayerName(playerName);
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Throttle interaction checks for performance
            if (Time.time >= nextInteractCheckTime)
            {
                nextInteractCheckTime = Time.time + interactCheckInterval;
                CheckForInteractables();
            }

            HandleInteraction();
        }

        /// <summary>
        /// Checks for nearby interactable objects using optimized physics queries.
        /// </summary>
        private void CheckForInteractables()
        {
            // Use NonAlloc version to avoid GC allocations
            int numColliders = Physics.OverlapSphereNonAlloc(transform.position, interactRadius, overlapBuffer);
            
            Interactable closestInteractable = null;
            float closestDistance = float.MaxValue;
            float halfAngle = interactAngle * 0.5f;

            for (int i = 0; i < numColliders; i++)
            {
                Interactable interactable = overlapBuffer[i].GetComponentInParent<Interactable>();
                if (interactable != null)
                {
                    Vector3 directionTo = (interactable.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, directionTo);
                    if (angle < halfAngle)
                    {
                        float distance = Vector3.Distance(transform.position, interactable.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestInteractable = interactable;
                        }
                    }
                }
            }

            currentClosestInteractable = closestInteractable;
            UpdateInteractableUI(closestInteractable);
        }

        /// <summary>
        /// Updates the UI for the currently highlighted interactable.
        /// </summary>
        private void UpdateInteractableUI(Interactable closestInteractable)
        {
            if (closestInteractable != lastHighlightedInteractable)
            {
                if (lastHighlightedInteractable != null)
                    lastHighlightedInteractable.HideUI();

                if (closestInteractable != null)
                    closestInteractable.ShowUI();

                lastHighlightedInteractable = closestInteractable;
            }
            else if (closestInteractable == null && lastHighlightedInteractable != null)
            {
                lastHighlightedInteractable.HideUI();
                lastHighlightedInteractable = null;
            }
        }

        /// <summary>
        /// Handles player interaction input for picking up and dropping objects.
        /// </summary>
        private void HandleInteraction()
        {
            if (currentClosestInteractable != null)
            {
                if (inputManager.interact && !isObjectPickedUp.Value)
                {
                    ServerInteractRpc(currentClosestInteractable.NetworkObjectId);
                    ServerPickupObjectRpc(currentClosestInteractable.NetworkObjectId);
                    inputManager.interact = false; // Prevent multiple pickups in one press
                }
            }

            if (inputManager.interact && isObjectPickedUp.Value)
            {
                ServerDropObjectRpc();
                inputManager.interact = false; // Prevent multiple drops in one press
            }
        }

        [Rpc(SendTo.Server)]
        private void ServerInteractRpc(ulong objectToInteractID)
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objectToInteractID, out var objectToInteract)) return;
            if (objectToInteract == null) return;

            Interactable interactableObject = objectToInteract.GetComponent<Interactable>();

            if (interactableObject == null) return;
            interactableObject.Interact();
        }

        [Rpc(SendTo.Server)]
        private void ServerPickupObjectRpc(ulong objToPickupID)
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objToPickupID, out var objectToPickup)) return;
            if (objectToPickup == null || objectToPickup.transform.parent != null) return;

            Pickable pickableObject = objectToPickup.GetComponent<Pickable>();
            if (pickableObject == null) return;

            if (objectToPickup.TrySetParent(transform))
            {
                isObjectPickedUp.Value = true;
                pickedUpObject = objectToPickup;
                pickedUpObject.transform.localPosition = pickableObject.holdPosition;
                pickedUpObject.transform.localRotation = pickableObject.holdRotation;
                pickableObject.OnObjectDespawn += ObjectDespawned;
            }
        }

        private void ObjectDespawned()
        {
            pickedUpObject = null;
            isObjectPickedUp.Value = false;
        }

        [Rpc(SendTo.Server)]
        private void ServerDropObjectRpc()
        {
            if (pickedUpObject != null)
            {
                pickedUpObject.GetComponent<Pickable>().OnObjectDespawn -= ObjectDespawned;
                pickedUpObject.TrySetParent(parent: (Transform)null);
                pickedUpObject = null;
            }
            isObjectPickedUp.Value = false;
        }
    }
}
#endif
