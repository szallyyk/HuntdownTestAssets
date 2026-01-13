#if UNITY_SERVICES || STEAM_SERVICES
using System;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Manages weapon handling, aiming, and IK for a networked player.
    /// </summary>
    public class WeaponManager : NetworkBehaviour
    {
        // Dependencies
        private Animator animator;
        private Camera playerCamera;
        private InputManager inputManager;
        private ThirdPersonController thirdPersonController;

        // Inspector references
        [SerializeField] private CinemachineCamera aimVirtualCamera;
        [SerializeField] private bool editorMode = false;

        // Weapon references
        [HideInInspector] public MeleeWeapon meleeWeapon;
        [HideInInspector] public ShooterWeapon shooterWeapon;
        public WeaponIk weaponIk;

        // Networked state
        private NetworkVariable<bool> isAim = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<Vector3> aimPoint = new NetworkVariable<Vector3>(
            Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [SerializeField] private float aimUpdateRate = 10f; // times per second
        private float nextSendTime;

        // Sensitivity settings
        public float normalSensitivity = 1f;
        public float aimSensitivity = 0.5f;

        // Weapon state
        public WeaponItem currentWeaponItem { get; private set; }
        private bool aimingMode = false;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            inputManager = GetComponent<InputManager>();
            playerCamera = Camera.main;
            thirdPersonController = GetComponent<ThirdPersonController>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner && aimVirtualCamera != null)
            {
                aimVirtualCamera.transform.SetParent(null);
            }
        }

        /// <summary>
        /// Assigns the current weapon based on the picked weapon.
        /// </summary>
        /// <param name="weapon">The picked weapon.</param>
        public void SetCurrentWeapon(Pickable weapon)
        {
            if (shooterWeapon != null)
            {
                currentWeaponItem = Array.Find(weaponIk.weapons, w => w.weaponId == shooterWeapon.WeaponId);
            }
            else if (meleeWeapon != null)
            {
                // TODO: Implement melee weapon assignment logic
            }
        }

        private void Update()
        {
            UpdateWeapon();
            if (!IsOwner) return;
            HandleInput();
        }

        /// <summary>
        /// Handles player input for aiming and shooting.
        /// </summary>
        private void HandleInput()
        {
            if (isAim.Value != inputManager.aim)
                isAim.Value = inputManager.aim;

            if (inputManager.action && isAim.Value)
            {
                shooterWeapon?.StartShoot();
            }
            else
            {
                shooterWeapon?.StopShoot();
            }
        }

        /// <summary>
        /// Updates weapon state and aiming logic.
        /// </summary>
        private void UpdateWeapon()
        {
            if (shooterWeapon == null) return;

            if (isAim.Value || aimingMode)
            {
                UpdateAimingState();
                if (thirdPersonController != null)
                {
                    thirdPersonController.SetCameraRotateSensitivity(aimSensitivity);
                    thirdPersonController.SetRotateOnMove(false);
                }

                RotateTowards(GetAimTargetPoint());
            }
            else
            {
                UpdateIdleState();
                if (thirdPersonController != null)
                {
                    thirdPersonController.SetCameraRotateSensitivity(normalSensitivity);
                    thirdPersonController.SetRotateOnMove(true);
                }
            }
        }

        [SerializeField] private float characterRotationSpeed = 20f;

        /// <summary>
        /// Rotates the character towards the aim target.
        /// </summary>
        private void RotateTowards(Vector3 targetPosition)
        {
            if (thirdPersonController == null) return;

            Vector3 worldAimTarget = targetPosition;
            worldAimTarget.y = thirdPersonController.transform.position.y;
            Vector3 aimDirection = (worldAimTarget - thirdPersonController.transform.position).normalized;
            thirdPersonController.transform.forward = Vector3.Lerp(
                thirdPersonController.transform.forward, aimDirection, Time.deltaTime * characterRotationSpeed);
        }

        /// <summary>
        /// Updates weapon and hand transforms for aiming.
        /// </summary>
        private void UpdateAimingState()
        {
            if (currentWeaponItem == null || editorMode) return;

            if (IsOwner && aimVirtualCamera != null)
                aimVirtualCamera.enabled = true;

            float smoothT = GetSmoothT();

            // Gun model position/rotation
            SmoothTransformLocal(shooterWeapon.GunModel, 
                currentWeaponItem.aimTransform.position, 
                currentWeaponItem.aimTransform.rotation, 
                smoothT);

            // Apply IK transforms for aiming
            ApplyIKTransform(shooterWeapon.RightHand, shooterWeapon.RightHandHint, 
                currentWeaponItem.rightHandAimIK, smoothT);
            ApplyIKTransform(shooterWeapon.LeftHand, shooterWeapon.LeftHandHint, 
                currentWeaponItem.leftHandAimIK, smoothT);

            // World rotation towards aim target
            Vector3 targetPoint = GetAimTargetPoint();
            Quaternion desiredWorldRot = GetDesiredWeaponRotation(targetPoint);

            if (shooterWeapon.Smooth)
            {
                shooterWeapon.GunModel.rotation = Quaternion.Slerp(
                    shooterWeapon.GunModel.rotation, desiredWorldRot, smoothT);
            }
            else
            {
                shooterWeapon.GunModel.rotation = desiredWorldRot;
            }
        }

        /// <summary>
        /// Updates weapon and hand transforms for idle/holding.
        /// </summary>
        private void UpdateIdleState()
        {
            if (IsOwner && aimVirtualCamera != null)
                aimVirtualCamera.enabled = false;

            if (currentWeaponItem == null || editorMode) return;

            float smoothT = GetSmoothT();

            // Gun model position/rotation
            SmoothTransformLocal(shooterWeapon.GunModel, 
                currentWeaponItem.holdTransform.position, 
                currentWeaponItem.holdTransform.rotation, 
                smoothT);

            // Apply IK transforms for holding
            ApplyIKTransform(shooterWeapon.RightHand, shooterWeapon.RightHandHint, 
                currentWeaponItem.rightHandIK, smoothT);
            ApplyIKTransform(shooterWeapon.LeftHand, shooterWeapon.LeftHandHint, 
                currentWeaponItem.leftHandIK, smoothT);
        }

        #region Helper Methods

        /// <summary>
        /// Calculates the smooth interpolation factor based on weapon speed and delta time.
        /// </summary>
        private float GetSmoothT()
        {
            return 1f - Mathf.Exp(-shooterWeapon.SmoothSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Smoothly interpolates a transform's local position and rotation.
        /// </summary>
        private void SmoothTransformLocal(Transform target, Vector3 targetPos, Quaternion targetRot, float t)
        {
            target.localPosition = Vector3.Lerp(target.localPosition, targetPos, t);
            target.localRotation = Quaternion.Slerp(target.localRotation, targetRot, t);
        }

        /// <summary>
        /// Applies smooth IK transform interpolation for hand and hint positions.
        /// </summary>
        private void ApplyIKTransform(Transform hand, Transform hint, IKTransform ikData, float t)
        {
            hand.localPosition = Vector3.Lerp(hand.localPosition, ikData.targetPos, t);
            hand.localRotation = Quaternion.Slerp(hand.localRotation, ikData.targetRot, t);
            hint.localPosition = Vector3.Lerp(hint.localPosition, ikData.hintPos, t);
        }

        #endregion

        /// <summary>
        /// Gets the world-space aim target point.
        /// </summary>
        /// 
        private Vector3 GetAimTargetPoint()
        {
            if (IsOwner)
            {
                Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                Vector3 targetPoint = centerRay.origin + centerRay.direction * shooterWeapon.DefaultAimDistance;

                if (Physics.Raycast(centerRay, out RaycastHit hit, shooterWeapon.DefaultAimDistance,
                shooterWeapon.AimLayerMask, QueryTriggerInteraction.Ignore))
                {
                    targetPoint = hit.point;
                }

                if (Time.time >= nextSendTime)
                {
                    nextSendTime = Time.time + (1f / aimUpdateRate);

                    // Only send if it actually changed
                    if ((aimPoint.Value - targetPoint).sqrMagnitude > 0.0025f)
                        aimPoint.Value = targetPoint;
                }
                return targetPoint;
            }
            else
            {
                return aimPoint.Value;
            }
        }

        /// <summary>
        /// Calculates the desired weapon rotation for aiming.
        /// </summary>
        private Quaternion GetDesiredWeaponRotation(Vector3 targetPoint)
        {
            Vector3 toTarget = targetPoint - shooterWeapon.GunModel.position;
            if (toTarget.sqrMagnitude < 0.0001f) return shooterWeapon.GunModel.rotation;

            Quaternion desiredWorldRot = Quaternion.LookRotation(toTarget.normalized, shooterWeapon.GunModel.up);
            desiredWorldRot *= Quaternion.Euler(shooterWeapon.AimOffsetEuler);

            if (shooterWeapon.ClampPitch)
            {
                Vector3 angles = NormalizeAngles(desiredWorldRot.eulerAngles);
                angles.x = Mathf.Clamp(angles.x, shooterWeapon.MinPitch, shooterWeapon.MaxPitch);
                desiredWorldRot = Quaternion.Euler(angles);
            }

            return desiredWorldRot;
        }

        /// <summary>
        /// Normalizes Euler angles to [-180, 180] range.
        /// </summary>
        private Vector3 NormalizeAngles(Vector3 e)
        {
            e.x = Mathf.Repeat(e.x + 180f, 360f) - 180f;
            e.y = Mathf.Repeat(e.y + 180f, 360f) - 180f;
            e.z = Mathf.Repeat(e.z + 180f, 360f) - 180f;
            return e;
        }

        /// <summary>
        /// Handles IK for weapon holding.
        /// </summary>
        private void OnAnimatorIK(int layerIndex)
        {
            if (shooterWeapon == null) return;

            SetIK(AvatarIKGoal.RightHand, shooterWeapon.RightHand, shooterWeapon.RightHandHint, AvatarIKHint.RightElbow);
            SetIK(AvatarIKGoal.LeftHand, shooterWeapon.LeftHand, shooterWeapon.LeftHandHint, AvatarIKHint.LeftElbow);
        }

        /// <summary>
        /// Sets IK positions and rotations for a hand.
        /// </summary>
        private void SetIK(AvatarIKGoal handGoal, Transform hand, Transform handHint, AvatarIKHint elbowHint)
        {
            animator.SetIKPositionWeight(handGoal, 1);
            animator.SetIKRotationWeight(handGoal, 1);
            animator.SetIKPosition(handGoal, hand.position);
            animator.SetIKRotation(handGoal, hand.rotation);
            animator.SetIKHintPositionWeight(elbowHint, 1);
            animator.SetIKHintPosition(elbowHint, handHint.position);
        }
    }
}
#endif