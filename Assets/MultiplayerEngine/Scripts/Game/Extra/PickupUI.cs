#if UNITY_SERVICES || STEAM_SERVICES
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Displays pickup UI above interactable objects and faces the camera.
    /// </summary>
    public class PickupUI : MonoBehaviour
    {
        public Image pickupButton;
        public Transform targetObject; // Assign the object this UI belongs to
        public Vector3 offset = new Vector3(0, 0.5f, 0); // Adjust this to move UI slightly up
        public float forwardOffset = 0.3f; // Distance in front of the object

        private Transform cameraTransform;

        private void Start()
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (targetObject == null)
            {
                targetObject = transform.parent;
            }
        }

        private void LateUpdate()
        {
            if (targetObject == null || cameraTransform == null) return;

            // Position UI above and in front of the object
            transform.position = targetObject.position + offset + targetObject.forward * forwardOffset;

            // Billboard: face the camera
            transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
        }
    }
}
#endif
