#if UNITY_SERVICES || STEAM_SERVICES
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Displays player stats UI (name, health) and faces the camera.
    /// </summary>
    public class PlayerStatsUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private Slider healthBar;

        private Transform cameraTransform;

        private void Start()
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            // Billboard: face the camera
            if (cameraTransform != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
            }
        }

        public void SetPlayerName(string name)
        {
            playerName.text = name;
        }

        /// <summary>
        /// Sets the health value (0-100 range by default).
        /// </summary>
        public void SetHealth(float healthValue)
        {
            if (healthBar != null)
                healthBar.value = healthValue;
        }

        /// <summary>
        /// Initialize the health bar with min, max, and current values.
        /// </summary>
        public void InitializeHealthBar(float minValue, float maxValue, float currentValue)
        {
            if (healthBar != null)
            {
                healthBar.minValue = minValue;
                healthBar.maxValue = maxValue;
                healthBar.value = currentValue;
            }
        }

        /// <summary>
        /// Sets the health bar min and max values.
        /// </summary>
        public void SetHealthBarRange(float minValue, float maxValue)
        {
            if (healthBar != null)
            {
                healthBar.minValue = minValue;
                healthBar.maxValue = maxValue;
            }
        }
    }
}
#endif
