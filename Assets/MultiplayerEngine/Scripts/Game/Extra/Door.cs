#if UNITY_SERVICES || STEAM_SERVICES
using UnityEngine;
namespace Ignitives.MultiplayerEngine
{
    public class Door : MonoBehaviour
    {
        [Header("Door Settings")]
        public Vector3 openOffset = new Vector3(0, 0, 2f); // How far to move when open
        public float moveSpeed = 2f;

        private bool isOpen = false;
        private bool isAnimating;
        private Vector3 closedPosition;
        private Vector3 openPosition;
        private float t;

        void Start()
        {
            closedPosition = transform.localPosition;
            openPosition = closedPosition + openOffset;
            transform.localPosition = isOpen ? openPosition : closedPosition;
            enabled = false; // Disable Update when not animating
        }

        void Update()
        {
            if (isAnimating)
            {
                t += Time.deltaTime * moveSpeed;
                transform.localPosition = Vector3.Lerp(
                    isOpen ? closedPosition : openPosition,
                    isOpen ? openPosition : closedPosition,
                    t
                );

                if (t >= 1f)
                {
                    isAnimating = false;
                    transform.localPosition = isOpen ? openPosition : closedPosition;
                    enabled = false; // Stop Update when animation completes
                }
            }
        }

        /// <summary>
        /// Toggles the door open/close state.
        /// </summary>
        public void ToggleDoor()
        {
            if (isAnimating) return;

            isOpen = !isOpen;
            t = 0f;
            isAnimating = true;
            enabled = true; // Enable Update to start animation
        }
    }
}
#endif
