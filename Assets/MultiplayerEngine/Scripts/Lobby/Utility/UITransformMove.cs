using System.Threading.Tasks;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    public class UITransformMove : MonoBehaviour
    {
        public RectTransform uiElement; // The UI element to move
        public Vector2 targetPosition; // The target localPosition relative to the start
        public float duration = 0.5f; // Duration for the movement
        private Vector2 startPosition;
        private Vector2 currentTargetPosition;
        private Coroutine movementCoroutine;
        private bool isOpen = false; // Tracks if the panel is open or closed

        void Start()
        {
            startPosition = uiElement.anchoredPosition; // Store the initial localPosition
            currentTargetPosition = startPosition; // Default to closed localPosition
        }

        public async Task<bool> ToggleMoveAsync()
        {
            // If a movement coroutine is running, stop it to allow interruption
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
            }

            // Switch target localPosition to either the original start or the target localPosition
            currentTargetPosition = isOpen ? startPosition : targetPosition;

            // Start the movement coroutine and await its completion
            await MoveUIAsync(currentTargetPosition);

            // Update the state and return whether the panel is open
            isOpen = !isOpen;
            return isOpen;
        }

        private async Task MoveUIAsync(Vector2 target)
        {
            Vector2 currentPos = uiElement.anchoredPosition;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                uiElement.anchoredPosition = Vector2.Lerp(currentPos, target, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                await Task.Yield();

                // If the target localPosition changes mid-movement, exit early
                if (target != currentTargetPosition)
                {
                    return;
                }
            }

            // Ensure it reaches the exact target localPosition
            uiElement.anchoredPosition = target;
        }

        private void Update()
        {
            if (isOpen && Input.GetMouseButtonDown(0))
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), Input.mousePosition, null))
                {
                    ToggleMoveAsync();
                }
            }
        }
    }
}
