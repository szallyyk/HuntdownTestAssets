#if STEAM_SERVICES || UNITY_SERVICES
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace Ignitives.MultiplayerEngine.Editor
{
    [CustomEditor(typeof(WeaponManager))]
    public class WeaponManagerEditor : UnityEditor.Editor
    {
        private bool showIKMenu = false;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty prop = serializedObject.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "weaponIk")
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(prop, true);
                    if (GUILayout.Button("Edit IK", GUILayout.Width(80)))
                    {
                        showIKMenu = !showIKMenu;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.PropertyField(prop, true);
                }
            }

            if (showIKMenu)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("IK Edit Mode: Toggle edit and debug aim modes below.", MessageType.Info);

                var targetObj = target;
                var type = targetObj.GetType();

                // Reflection for private fields
                var editorModeField = type.GetField("editorMode", BindingFlags.NonPublic | BindingFlags.Instance);
                var aimingModeField = type.GetField("aimingMode", BindingFlags.NonPublic | BindingFlags.Instance);

                bool editorMode = editorModeField != null ? (bool)editorModeField.GetValue(targetObj) : false;
                bool aimingMode = aimingModeField != null ? (bool)aimingModeField.GetValue(targetObj) : false;

                EditorGUILayout.BeginHorizontal();
                bool newEditorMode = EditorGUILayout.Toggle("Enable Edit Mode", editorMode);
                EditorGUILayout.LabelField(newEditorMode ? "On" : "Off", GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
                if (editorModeField != null && newEditorMode != editorMode)
                {
                    editorModeField.SetValue(targetObj, newEditorMode);
                    EditorUtility.SetDirty(targetObj);
                }

                EditorGUILayout.BeginHorizontal();
                bool newAimingMode = EditorGUILayout.Toggle("Debug Aim Mode", aimingMode);
                EditorGUILayout.LabelField(newAimingMode ? "On" : "Off", GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
                if (aimingModeField != null && newAimingMode != aimingMode)
                {
                    aimingModeField.SetValue(targetObj, newAimingMode);
                    EditorUtility.SetDirty(targetObj);
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Save Aim IK", GUILayout.Height(30)))
                {
                    var weaponManager = (WeaponManager)target;
                    var shooterWeapon = weaponManager.shooterWeapon;
                    var weaponIk = weaponManager.weaponIk;

                    if (shooterWeapon != null && weaponIk != null)
                    {
                        // Find WeaponItem by weaponId
                        var weaponItem = System.Array.Find(weaponIk.weapons, w => w.weaponId == shooterWeapon.WeaponId);
                        if (weaponItem != null)
                        {
                            // Save Aim Transform
                            weaponItem.aimTransform.position = shooterWeapon.GunModel.localPosition;
                            weaponItem.aimTransform.rotation = shooterWeapon.GunModel.localRotation;

                            // Save Aim IK for hands
                            weaponItem.rightHandAimIK.targetPos = shooterWeapon.RightHand.localPosition;
                            weaponItem.rightHandAimIK.targetRot = shooterWeapon.RightHand.localRotation;
                            weaponItem.rightHandAimIK.hintPos = shooterWeapon.RightHandHint.localPosition;

                            weaponItem.leftHandAimIK.targetPos = shooterWeapon.LeftHand.localPosition;
                            weaponItem.leftHandAimIK.targetRot = shooterWeapon.LeftHand.localRotation;
                            weaponItem.leftHandAimIK.hintPos = shooterWeapon.LeftHandHint.localPosition;

                            EditorUtility.SetDirty(weaponIk);
                            AssetDatabase.SaveAssets();

                            var updateIKMethod = typeof(WeaponManager).GetMethod("UpdateNewIK", BindingFlags.NonPublic | BindingFlags.Instance);
                            updateIKMethod?.Invoke(weaponManager, null);
                        }
                    }
                }

                if (GUILayout.Button("Save Hold IK", GUILayout.Height(30)))
                {
                    var weaponManager = (WeaponManager)target;
                    var shooterWeapon = weaponManager.shooterWeapon;
                    var weaponIk = weaponManager.weaponIk;

                    if (shooterWeapon != null && weaponIk != null)
                    {
                        // Find WeaponItem by weaponId
                        var weaponItem = System.Array.Find(weaponIk.weapons, w => w.weaponId == shooterWeapon.WeaponId);
                        if (weaponItem != null)
                        {
                            // Save Hold Transform
                            weaponItem.holdTransform.position = shooterWeapon.GunModel.localPosition;
                            weaponItem.holdTransform.rotation = shooterWeapon.GunModel.localRotation;

                            // Save Hold IK for hands
                            weaponItem.rightHandIK.targetPos = shooterWeapon.RightHand.localPosition;
                            weaponItem.rightHandIK.targetRot = shooterWeapon.RightHand.localRotation;
                            weaponItem.rightHandIK.hintPos = shooterWeapon.RightHandHint.localPosition;

                            weaponItem.leftHandIK.targetPos = shooterWeapon.LeftHand.localPosition;
                            weaponItem.leftHandIK.targetRot = shooterWeapon.LeftHand.localRotation;
                            weaponItem.leftHandIK.hintPos = shooterWeapon.LeftHandHint.localPosition;

                            EditorUtility.SetDirty(weaponIk);
                            AssetDatabase.SaveAssets();

                            var updateIKMethod = typeof(WeaponManager).GetMethod("UpdateNewIK", BindingFlags.NonPublic | BindingFlags.Instance);
                            updateIKMethod?.Invoke(weaponManager, null);
                        }
                    }
                }

                if (GUILayout.Button("Close Menu"))
                {
                    showIKMenu = false;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif