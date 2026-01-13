#if UNITY_SERVICES || STEAM_SERVICES
using Unity.Netcode;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Represents a melee weapon that can be picked up and attached to a player's hand.
    /// </summary>
    public class MeleeWeapon : Pickable
    {
        [Header("Weapon Setup")]
        [SerializeField] private Transform weaponModel;
        [SerializeField] private HumanBodyBones weaponHolder = HumanBodyBones.RightHand;
        [SerializeField] private Vector3 pickupPosition;
        [SerializeField] private Quaternion pickupRotation = Quaternion.identity;

        [Header("Animation")]
        [SerializeField] private string attackAnimationTrigger = "MeleeAttack";
        [SerializeField] private float attackCooldown = 0.5f;

        private WeaponManager weaponManager;
        private Animator playerAnimator;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private bool isInitialized = false;
        private float lastAttackTime = 0f;

        private void Start()
        {
            CacheOriginalTransform();
        }

        private void CacheOriginalTransform()
        {
            if (weaponModel != null && !isInitialized)
            {
                originalPosition = weaponModel.localPosition;
                originalRotation = weaponModel.localRotation;
                isInitialized = true;
            }
        }

        /// <summary>
        /// Triggers the melee attack animation.
        /// </summary>
        public void Attack()
        {
            if (playerAnimator == null) return;
            if (Time.time - lastAttackTime < attackCooldown) return;

            lastAttackTime = Time.time;
            playerAnimator.SetTrigger(attackAnimationTrigger);
        }

        /// <summary>
        /// Called by animation event to deal damage.
        /// </summary>
        public void OnAttackHit()
        {
            // Override this in derived classes or use Unity Events for damage dealing
        }

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            base.OnNetworkObjectParentChanged(parentNetworkObject);

            if (parentNetworkObject != null)
            {
                AttachToPlayer();
            }
            else
            {
                DetachFromPlayer();
            }
        }

        private void AttachToPlayer()
        {
            if (weaponModel == null)
            {
                Debug.LogWarning("MeleeWeapon: weaponModel is not assigned.", this);
                return;
            }

            CacheOriginalTransform();

            playerAnimator = GetComponentInParent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogWarning("MeleeWeapon: No Animator found in parent hierarchy.", this);
                return;
            }

            Transform boneTransform = playerAnimator.GetBoneTransform(weaponHolder);
            if (boneTransform == null)
            {
                Debug.LogWarning($"MeleeWeapon: Bone '{weaponHolder}' not found on animator.", this);
                return;
            }

            weaponModel.SetParent(boneTransform);
            weaponModel.localPosition = pickupPosition;
            weaponModel.localRotation = pickupRotation;

            weaponManager = GetComponentInParent<WeaponManager>();
            if (weaponManager != null)
            {
                weaponManager.meleeWeapon = this;
                weaponManager.SetCurrentWeapon(this);
            }
        }

        private void DetachFromPlayer()
        {
            if (weaponModel == null) return;

            weaponModel.SetParent(transform);
            weaponModel.localPosition = originalPosition;
            weaponModel.localRotation = originalRotation;

            if (weaponManager != null && weaponManager.meleeWeapon == this)
            {
                weaponManager.meleeWeapon = null;
            }
            weaponManager = null;
            playerAnimator = null;
        }
    }
}
#endif
