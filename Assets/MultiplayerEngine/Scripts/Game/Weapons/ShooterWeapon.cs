#if UNITY_SERVICES || STEAM_SERVICES
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Represents a shooter weapon that can be picked up and fired by a player.
    /// </summary>
    public class ShooterWeapon : Pickable
    {
        [Header("References")]
        [SerializeField] private string weaponId;
        [SerializeField] private Transform gunModel;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private HumanBodyBones weaponHolder = HumanBodyBones.Chest; 

        [Header("Hand IK Targets")]
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform rightHandHint;
        [SerializeField] private Transform leftHandHint;

        [Header("Raycast/Aim Settings")]
        [SerializeField] private float defaultAimDistance = 100f;
        [SerializeField] private LayerMask aimLayerMask = ~0;

        [Header("Rotation")]
        [SerializeField] private bool smooth = true;
        [SerializeField] private float smoothSpeed = 15f;
        [SerializeField] private Vector3 aimOffsetEuler = Vector3.zero;
        [SerializeField] private bool clampPitch = true;
        [SerializeField] private float minPitch = -60f;
        [SerializeField] private float maxPitch = 60f;

        [Header("Fire Settings")]
        [SerializeField] private float fireRate = 0.1f;
        [SerializeField] private ShootMode firingMode = ShootMode.Auto;

        /// <summary>Gets the gun model transform.</summary>
        public Transform GunModel => gunModel;
        /// <summary>Gets the bone where the weapon is held.</summary>
        public HumanBodyBones Holder => weaponHolder;
        /// <summary>Gets the weapon ID.</summary>
        public string WeaponId => weaponId;
        /// <summary>Gets the left hand IK target.</summary>
        public Transform LeftHand => leftHand;
        /// <summary>Gets the right hand IK target.</summary>
        public Transform RightHand => rightHand;
        /// <summary>Gets the right hand hint IK target.</summary>
        public Transform RightHandHint => rightHandHint;
        /// <summary>Gets the left hand hint IK target.</summary>
        public Transform LeftHandHint => leftHandHint;

        public float DefaultAimDistance => defaultAimDistance;
        public LayerMask AimLayerMask => aimLayerMask;
        public bool Smooth => smooth;
        public float SmoothSpeed => smoothSpeed;
        public Vector3 AimOffsetEuler => aimOffsetEuler;
        public bool ClampPitch => clampPitch;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;

        private ObjectPool<Projectile> projectilePool;
        private Vector3 originalGunPosition;
        private Quaternion originalGunRotation;
        private WeaponManager weaponManager;
        private Coroutine shootingCoroutine;
        private bool isShooting = false;

        private void Awake()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError("Projectile prefab is not assigned.", this);
                return;
            }

            projectilePool = new ObjectPool<Projectile>(
                () => Instantiate(projectilePrefab),
                bullet => bullet.gameObject.SetActive(true),
                bullet => bullet.gameObject.SetActive(false),
                bullet => Destroy(bullet.gameObject),
                false, 10, 100
            );
        }

        private void Start()
        {
            if (gunModel != null)
            {
                originalGunPosition = gunModel.localPosition;
                originalGunRotation = gunModel.localRotation;
            }
        }

        /// <summary>
        /// Starts shooting based on the firing mode.
        /// </summary>
       
        public void StartShoot()
        {
            if (projectilePrefab == null || projectilePool == null) return;

            if (firingMode == ShootMode.Auto)
            {
                if (!isShooting && shootingCoroutine == null)
                {
                    isShooting = true;
                    shootingCoroutine = StartCoroutine(AutoFire());
                }
            }
            else if (firingMode == ShootMode.Single)
            {
                if (!isShooting)
                {
                    isShooting = true;
                    ShootRpc();
                }
            }
        }

        /// <summary>
        /// Stops automatic shooting.
        /// </summary>
       
        public void StopShoot()
        {
            isShooting = false;
            if (shootingCoroutine != null)
            {
                StopCoroutine(shootingCoroutine);
                shootingCoroutine = null;
            }
        }

        private IEnumerator AutoFire()
        {
            while (isShooting)
            {
                ShootRpc();
                yield return new WaitForSeconds(fireRate);
            }
            shootingCoroutine = null;
        }

        /// <summary>
        /// Fires a projectile and handles effects.
        /// </summary>
        [Rpc(SendTo.Everyone)]
        public void ShootRpc()
        {
            if (muzzleFlash != null)
                muzzleFlash.Play();

            Vector3 targetPoint = shootPoint.position + shootPoint.forward * defaultAimDistance;
            GameObject hitObject = null;

            Ray ray = new Ray(shootPoint.position, shootPoint.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, defaultAimDistance, aimLayerMask))
            {
                targetPoint = hit.point;
                hitObject = hit.collider.gameObject;
            }

            if (projectilePrefab != null && projectilePool != null)
            {
                Projectile bullet = projectilePool.Get();
                bullet.transform.position = shootPoint.position;

                // Clear trail on reuse
                bullet.ClearTrail();

                bullet.transform.rotation = Quaternion.LookRotation(targetPoint - shootPoint.position);

                bullet.MoveTo(targetPoint, 50f, proj =>
                {
                    if (hitObject != null)
                    {
                        DamageMessage damageMessage = new DamageMessage(transform, targetPoint, bullet.Damage, weaponId);
                        hitObject.SendMessage("TakeDamage", damageMessage, SendMessageOptions.DontRequireReceiver);
                    }
                    projectilePool.Release(proj);
                });
            }
        }

        /// <summary>
        /// Handles changes in network object parenting.
        /// </summary>
        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            base.OnNetworkObjectParentChanged(parentNetworkObject);

            if (parentNetworkObject != null)
            {
                Animator animator = GetComponentInParent<Animator>();
                if (animator != null && gunModel != null)
                {
                    Transform spineTransform = animator.GetBoneTransform(HumanBodyBones.Spine);
                    if (spineTransform != null)
                        gunModel.SetParent(spineTransform);
                }

                weaponManager = GetComponentInParent<WeaponManager>();
                if (weaponManager != null)
                {
                    weaponManager.shooterWeapon = this;
                    weaponManager.SetCurrentWeapon(this);
                }
            }
            else
            {
                StopShoot();
                if (gunModel != null)
                {
                    gunModel.SetParent(transform);
                    gunModel.localPosition = originalGunPosition;
                    gunModel.localRotation = originalGunRotation;
                }
                if (weaponManager != null && weaponManager.shooterWeapon == this)
                    weaponManager.shooterWeapon = null;
            }
        }
    }

    /// <summary>
    /// Defines the firing mode for the shooter weapon.
    /// </summary>
    public enum ShootMode
    {
        Auto,
        Single
    }
}
#endif