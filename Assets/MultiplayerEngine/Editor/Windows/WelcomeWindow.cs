using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Ignitives.MultiplayerEngine
{
    /// <summary>
    /// Welcome window shown at Unity startup for Multiplayer Engine.
    /// Includes intro + backend setup pages.
    /// </summary>
    [InitializeOnLoad]
    public class WelcomeWindow : EditorWindow
    {
        private static string steamPackage = "https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#20.0.0";

        private static string[] unityServices = new string[]
        {
            "com.unity.services.multiplayer",
            "com.unity.services.friends",
            "com.unity.services.authentication",
            "com.unity.services.cloudsave",
            "com.unity.services.vivox"
        };

        private static int currentPage = 0;
        private bool dontShowAgain;

        // Add this field to store scroll position
        private Vector2 scrollPos;

        static WelcomeWindow()
        {
            EditorApplication.update += ShowOnStartup;
        }

        private static void ShowOnStartup()
        {
            EditorApplication.update -= ShowOnStartup;
            if (EditorPrefs.GetBool("WelcomeWindow_DontShow", false))
                return;
            ShowWindow();
        }

        [MenuItem("Tools/Multiplayer Engine/Welcome Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<WelcomeWindow>(true, "Welcome!!", true);
            window.minSize = new Vector2(500, 775);
        }

        [MenuItem("Tools/Multiplayer Engine/Switch Backend")]
        public static void ShowBackendPage()
        {
            var window = GetWindow<WelcomeWindow>(true, "Change Backend", true);
            window.minSize = new Vector2(500, 775);
            currentPage = 1;
        }

        private void OnEnable()
        {
            dontShowAgain = EditorPrefs.GetBool("WelcomeWindow_DontShow", false);
        }

        private void OnGUI()
        {
            // Begin scroll view
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (currentPage == 0)
                DrawPageOne();
            else
                DrawPageTwo();

            GUILayout.EndScrollView(); // End scroll view

            DrawFooterNavigation();
        }

        #region PAGE ONE
        private void DrawPageOne()
        {
            GUILayout.Space(15);

            Texture2D coverImage = (Texture2D)EditorGUIUtility.Load("Assets/MultiplayerEngine/Editor/Images/Background.png");
            if (coverImage)
            {
                Rect cardRect = GUILayoutUtility.GetAspectRect((float)coverImage.width / coverImage.height, GUILayout.ExpandWidth(true));
                GUI.DrawTexture(cardRect, coverImage, ScaleMode.ScaleAndCrop, true);
            }

            GUILayout.Space(15);

            // Card style for dark grey background
            GUIStyle cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(20, 20, 20, 20),
                margin = new RectOffset(20, 20, 0, 0),
                normal = { background = MakeTex(2, 2, new Color(0.16f, 0.18f, 0.22f)) }
            };

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 26,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.95f, 1f) }
            };

            GUIStyle descStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            GUILayout.BeginVertical(cardStyle, GUILayout.ExpandWidth(true));
            GUILayout.Label("Multiplayer Engine", titleStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            GUILayout.Label(
                "Thank you for using Multiplayer Engine!\n\n" +
                "This asset includes everything you need to kickstart your multiplayer project — It features authentication, lobbies, a friend system, voice chat with proximity support, and a third-person controller with basic melee and shooter mechanics." +
                "Get started quickly with pre - built scripts and systems, allowing you to focus on crafting your unique multiplayer experience.\n\n" +
                "For any help, feel free to contact us on Discord and stay updated with the latest improvements.",
                descStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(20);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                fixedHeight = 40
            };

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🌐 Visit Website", buttonStyle, GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL("https://ignitives-organization.gitbook.io/multiplayer-engine-lite/");
            }
            if (GUILayout.Button("💬 Join Discord", buttonStyle, GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL("https://discord.gg/59cFVYavpd");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(25);

            // "Next" functionality moved to label-as-button
            GUIStyle nextLabelButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                fixedHeight = 32,
                wordWrap = true
            };

            if (GUILayout.Button("Let’s set up your backend service next →", nextLabelButtonStyle, GUILayout.ExpandWidth(true)))
            {
                currentPage = 1;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
        #endregion

        #region PAGE TWO (BACKEND SWITCHER)
        private void DrawPageTwo()
        {
            GUILayout.Space(15);

            // Cover image (same as first page)
            Texture2D coverImage = (Texture2D)EditorGUIUtility.Load("Assets/MultiplayerEngine/Editor/Images/Backend.png");
            if (coverImage)
            {
                Rect cardRect = GUILayoutUtility.GetAspectRect((float)coverImage.width / coverImage.height, GUILayout.ExpandWidth(true));
                GUI.DrawTexture(cardRect, coverImage, ScaleMode.ScaleAndCrop, true);
            }

            GUILayout.Space(15);

            // Card style for section
            GUIStyle cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(20, 20, 20, 20),
                margin = new RectOffset(20, 20, 0, 0),
                normal = { background = MakeTex(2, 2, new Color(0.16f, 0.18f, 0.22f)) }
            };

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 26,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.95f, 1f) }
            };

            GUIStyle descStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            GUILayout.BeginVertical(cardStyle, GUILayout.ExpandWidth(true));
            GUILayout.Label("Backend Selection", titleStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            GUILayout.Label(
                "Choose your multiplayer backend. You can select either Steam or Unity Services. Only one backend can be active at a time.",
                descStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(20);

            // Switch buttons
            bool isSteam = IsDefineSet("STEAM_SERVICES");
            bool isUnity = IsDefineSet("UNITY_SERVICES");

            GUIStyle switchButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                fixedHeight = 40,
                normal = { textColor = Color.white }
            };
            GUIStyle activeSwitchButtonStyle = new GUIStyle(switchButtonStyle)
            {
                normal = { background = MakeTex(2, 2, new Color(0.2f, 0.5f, 0.2f)) }
            };

            GUILayout.BeginHorizontal();
            if (isSteam)
            {
                GUILayout.Button("Steam", activeSwitchButtonStyle, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Unity Services", switchButtonStyle, GUILayout.ExpandWidth(true)))
                    InstallUnityServices();
            }
            else if (isUnity)
            {
                if (GUILayout.Button("Steam", switchButtonStyle, GUILayout.ExpandWidth(true)))
                    InstallSteam();
                GUILayout.Button("Unity Services", activeSwitchButtonStyle, GUILayout.ExpandWidth(true));
            }
            else
            {
                if (GUILayout.Button("Steam", switchButtonStyle, GUILayout.ExpandWidth(true)))
                    InstallSteam();
                if (GUILayout.Button("Unity Services", switchButtonStyle, GUILayout.ExpandWidth(true)))
                    InstallUnityServices();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(25);

            // Status display
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Texture2D greenCircle = EditorGUIUtility.IconContent("TestPassed").image as Texture2D;
            Texture2D defaultCircle = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
            GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };

            if (isSteam)
            {
                GUILayout.Label(new GUIContent(" Steam: Active", greenCircle), statusStyle, GUILayout.Height(32));
            }
            else if (isUnity)
            {
                GUILayout.Label(new GUIContent(" Unity: Active", greenCircle), statusStyle, GUILayout.Height(32));
            }
            else
            {
                GUILayout.Label(new GUIContent(" No backend selected", defaultCircle), statusStyle, GUILayout.Height(32));
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(25);

            // Document buttons
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                fixedHeight = 40
            };

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Online Documentation (Multiplayer Engine)", buttonStyle))
                Application.OpenURL("https://ignitives-organization.gitbook.io/multiplayer-engine-lite/");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Steam Docs", buttonStyle))
                Application.OpenURL("https://steamworks.github.io/");
            if (GUILayout.Button("Unity Docs", buttonStyle))
                Application.OpenURL("https://docs.unity.com/");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Steam Dashboard", buttonStyle))
                Application.OpenURL("https://partner.steamgames.com/");
            if (GUILayout.Button("Unity Dashboard", buttonStyle))
                Application.OpenURL("https://dashboard.unity3d.com/");
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Don't Show Again toggle
            GUILayout.BeginHorizontal();
            bool newDontShowAgain = EditorGUILayout.Toggle("Don't Show Again", dontShowAgain);
            if (newDontShowAgain != dontShowAgain)
            {
                dontShowAgain = newDontShowAgain;
                EditorPrefs.SetBool("WelcomeWindow_DontShow", dontShowAgain);
            }
            GUILayout.EndHorizontal();
        }
        #endregion

        #region FOOTER NAVIGATION
        private void DrawFooterNavigation()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            if (currentPage == 1)
            {
                if (GUILayout.Button("← Back", GUILayout.Height(30), GUILayout.Width(100)))
                    currentPage = 0;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Finish", GUILayout.Height(30), GUILayout.Width(100)))
                    Close();
            }
            else
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Next →", GUILayout.Height(30), GUILayout.Width(100)))
                    currentPage = 1;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }
        #endregion

        #region HELPERS

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = color;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private static void InstallSteam()
        {
            foreach (var pkg in unityServices)
                Client.Remove(pkg);

            Client.Add(steamPackage);
            SetScriptingDefines("STEAM_SERVICES", "UNITY_SERVICES");
            Debug.Log("Steamworks.NET installed. UNITY_SERVICES define removed, STEAM_SERVICES added.");
        }

        private static void InstallUnityServices()
        {
            Client.Remove("com.rlabrecque.steamworks.net");
            foreach (var pkg in unityServices)
                Client.Add(pkg);

            SetScriptingDefines("UNITY_SERVICES", "STEAM_SERVICES");
            Debug.Log("Unity Services installed. STEAM_SERVICES define removed, UNITY_SERVICES added.");
        }

        private static void SetScriptingDefines(string addDefine, string removeDefine)
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

            List<string> defineList = new List<string>(defines.Split(';'));
            defineList.RemoveAll(d => string.IsNullOrWhiteSpace(d));
            defineList.RemoveAll(d => d == removeDefine);
            if (!defineList.Contains(addDefine))
                defineList.Add(addDefine);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", defineList));
        }

        private static bool IsDefineSet(string define)
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            var defineList = new List<string>(defines.Split(';'));
            return defineList.Contains(define);
        }

        #endregion
    }
}
