#if UNITY_SERVICES || STEAM_SERVICES
using System;
using Unity.Netcode;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    public class HealthManager : NetworkBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int minHealth = 0;

        public NetworkVariable<int> Health = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private PlayerStatsUI _playerStatsUI;

        public int MaxHealth => maxHealth;
        public int MinHealth => minHealth;

        private void Awake()
        {
            _playerStatsUI = GetComponentInChildren<PlayerStatsUI>();
            Health.OnValueChanged += OnHealthChanged;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Initialize health bar with min/max values
            if (_playerStatsUI != null)
            {
                _playerStatsUI.InitializeHealthBar(minHealth, maxHealth, Health.Value);
            }

            // Set initial health on server
            if (IsServer)
            {
                Health.Value = maxHealth;
            }
        }

        private void OnHealthChanged(int previousValue, int newValue)
        {
            if (_playerStatsUI != null)
            {
                _playerStatsUI.SetHealth(newValue);
            }
        }

        public void TakeDamage(DamageMessage damageMessage)
        {
            if (!IsServer) return;
            Health.Value = Mathf.Max(minHealth, Health.Value - damageMessage.damage);
            if (Health.Value <= minHealth)
            {
                // Handle character death
                OnDeath();
            }
        }

        /// <summary>
        /// Heals the character by the specified amount.
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsServer) return;
            Health.Value = Mathf.Min(maxHealth, Health.Value + amount);
        }

        /// <summary>
        /// Sets health to max value.
        /// </summary>
        public void ResetHealth()
        {
            if (!IsServer) return;
            Health.Value = maxHealth;
        }

        /// <summary>
        /// Called when health reaches minimum. Override for custom death behavior.
        /// </summary>
        protected virtual void OnDeath()
        {
            // Override in derived classes for custom death handling
        }
    }

    public struct DamageMessage
    {
        public Transform sender;
        public Vector3 hitPoint;
        public int damage;
        public string source;

        public DamageMessage(Transform sender, Vector3 hitPoint, int damage, string source = "")
        {
            this.sender = sender;
            this.hitPoint = hitPoint;
            this.damage = damage;
            this.source = source;
        }
    }
}
#endif