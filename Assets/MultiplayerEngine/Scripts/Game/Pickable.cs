#if UNITY_SERVICES || STEAM_SERVICES
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Ignitives.MultiplayerEngine
{
    public class Pickable : Interactable
    {
        public Vector3 holdPosition;
        public Quaternion holdRotation;

        public event System.Action OnObjectDespawn;
        public Rigidbody rigidBody;
        public UnityEvent OnPickedup;

        public PickupUI pickupUI;

        public bool isPickedUp { get; private set; } = false;

        private void Awake()
        {
            // Hide pickup UI at start - it will show when player is near
            HideUI();
        }

        public override void ShowUI()
        {
            if( isPickedUp || pickupUI == null) return;
            pickupUI.gameObject.SetActive(true);
        }

        public override void HideUI()
        {
            if (pickupUI == null) return;
            pickupUI.gameObject.SetActive(false);
        }

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            base.OnNetworkObjectParentChanged(parentNetworkObject);
            SetKinematic(parentNetworkObject != null);
        }

        private void SetKinematic(bool isEnabled)
        {
            if (rigidBody == null) rigidBody = GetComponent<Rigidbody>();
            rigidBody.isKinematic = isEnabled;
            rigidBody.interpolation = isEnabled ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
            isPickedUp = isEnabled;
            if( isEnabled ) HideUI();
            if (isEnabled) OnPickedup?.Invoke();
        }



        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            OnObjectDespawn?.Invoke();
        }
    }
}
#endif
