#if UNITY_SERVICES || STEAM_SERVICES
using System;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    [CreateAssetMenu(fileName = "WeaponIK", menuName = "Ignitives/MultiplayerEngine/WeaponIK")]
    public class WeaponIk : ScriptableObject
    {
        public WeaponItem[] weapons;
    }
    [Serializable]
    public class WeaponItem
    {
        public string weaponId;

        public IKTransform rightHandIK;
        public IKTransform leftHandIK;

        public IKTransform rightHandAimIK;
        public IKTransform leftHandAimIK;

        public WeaponTransform holdTransform;
        public WeaponTransform aimTransform;
    }

    [Serializable]
    public class IKTransform
    {
        public Vector3 targetPos;
        public Quaternion targetRot;
        public Vector3 hintPos;
    }

    [Serializable]
    public class WeaponTransform
    {
        public Vector3 position;
        public Quaternion rotation;
    }

}
#endif

