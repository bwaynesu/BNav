using System.IO;
using UnityEditor;
using UnityEngine;

namespace BTools.BNav.Editor
{
    /// <summary>
    /// Project settings integration for BNav
    /// </summary>
    public static class BNavProjectSettings
    {
        [System.Serializable]
        private class BNavProjectSettingsData
        {
            public BNavGlobalSettings defaultGlobalSettings;
        }

        private static BNavProjectSettingsData projectSettings;

        private const string SettingsPath = "ProjectSettings/BNavSettings.asset";
        private const string DefaultSettingsPath = "Assets/Resources/BNavGlobalSettings.asset";
        private const string SettingsMenuPath = "Project/BNav Settings";

        /// <summary>
        /// Get the default global settings
        /// </summary>
        /// <returns>Default global settings ScriptableObject</returns>
        public static BNavGlobalSettings GetDefaultGlobalSettings()
        {
            var settings = GetProjectSettings();

            if (settings.defaultGlobalSettings == null)
            {
                // Try to find existing settings
                settings.defaultGlobalSettings = FindOrCreateDefaultSettings();
                SaveProjectSettings(settings);
            }

            // Also set it in the runtime loader
            BNavSettingsLoader.GlobalSettings = settings.defaultGlobalSettings;

            return settings.defaultGlobalSettings;
        }

        /// <summary>
        /// Set the default global settings
        /// </summary>
        /// <param name="globalSettings">Global settings to set as default</param>
        public static void SetDefaultGlobalSettings(BNavGlobalSettings globalSettings)
        {
            var settings = GetProjectSettings();

            settings.defaultGlobalSettings = globalSettings;
            SaveProjectSettings(settings);

            // Also set it in the runtime loader
            BNavSettingsLoader.GlobalSettings = globalSettings;
        }

        /// <summary>
        /// Get the project settings data
        /// </summary>
        /// <returns>Project settings data</returns>
        private static BNavProjectSettingsData GetProjectSettings()
        {
            if (projectSettings == null)
            {
                projectSettings = LoadProjectSettings();
            }

            return projectSettings;
        }

        /// <summary>
        /// Load project settings from file
        /// </summary>
        /// <returns>Loaded project settings data</returns>
        private static BNavProjectSettingsData LoadProjectSettings()
        {
            var data = new BNavProjectSettingsData();

            if (File.Exists(SettingsPath))
            {
                try
                {
                    var json = File.ReadAllText(SettingsPath);
                    JsonUtility.FromJsonOverwrite(json, data);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load BNav project settings: {e.Message}");
                }
            }

            return data;
        }

        /// <summary>
        /// Save project settings to file
        /// </summary>
        /// <param name="data">Project settings data to save</param>
        private static void SaveProjectSettings(BNavProjectSettingsData data)
        {
            try
            {
                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SettingsPath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save BNav project settings: {e.Message}");
            }
        }

        /// <summary>
        /// Find or create the default settings asset
        /// </summary>
        /// <returns>Default settings ScriptableObject</returns>
        private static BNavGlobalSettings FindOrCreateDefaultSettings()
        {
#if UNITY_EDITOR
            // Try to find existing settings
            var guids = AssetDatabase.FindAssets("t:BNavGlobalSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var existingSettings = AssetDatabase.LoadAssetAtPath<BNavGlobalSettings>(path);

                if (existingSettings != null)
                {
                    return existingSettings;
                }
            }

            // Create new settings
            var newSettings = ScriptableObject.CreateInstance<BNavGlobalSettings>();

            // Add default groups
            newSettings.AddGroup("Default");
            newSettings.AddGroup("Game");
            newSettings.AddGroup("Menu");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(DefaultSettingsPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save the asset
            AssetDatabase.CreateAsset(newSettings, DefaultSettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return newSettings;
#else
            return ScriptableObject.CreateInstance<BNavGlobalSettings>();
#endif
        }

#if UNITY_EDITOR

        /// <summary>
        /// Settings provider for Unity's Project Settings window
        /// </summary>
        /// <returns>Settings provider</returns>
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider(SettingsMenuPath, SettingsScope.Project)
            {
                label = "BNav Settings",
                guiHandler = (searchContext) => DrawProjectSettings(),
                keywords = new[] { "BNav", "Navigation", "UI", "BTools" }
            };

            return provider;
        }

        /// <summary>
        /// Draw the project settings GUI
        /// </summary>
        private static void DrawProjectSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("BNav Project Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var settings = GetProjectSettings();

            EditorGUI.BeginChangeCheck();

            // Default Global Settings field
            var newDefaultSettings = (BNavGlobalSettings)EditorGUILayout.ObjectField(
                new GUIContent("Default Global Settings", "The default BNavGlobalSettings asset to use for new BNavigation components"),
                settings.defaultGlobalSettings,
                typeof(BNavGlobalSettings),
                false
            );

            if (EditorGUI.EndChangeCheck())
            {
                settings.defaultGlobalSettings = newDefaultSettings;
                SaveProjectSettings(settings);
            }

            EditorGUILayout.Space();

            // Create new settings button
            if (GUILayout.Button("Create New Global Settings", GUILayout.Height(30)))
            {
                CreateNewGlobalSettings();
            }

            // Open settings editor button
            if (settings.defaultGlobalSettings != null && 
                GUILayout.Button("Edit Global Settings", GUILayout.Height(30)))
            {
                Selection.activeObject = settings.defaultGlobalSettings;
                EditorGUIUtility.PingObject(settings.defaultGlobalSettings);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "BNav uses global settings to define navigation rules between different UI groups. " +
                "Set a default global settings asset here, and it will be automatically assigned to all BNavigation components.",
                MessageType.Info
            );
        }

        /// <summary>
        /// Create a new global settings asset
        /// </summary>
        private static void CreateNewGlobalSettings()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create BNav Global Settings",
                "BNavGlobalSettings",
                "asset",
                "Choose location for new BNav Global Settings"
            );

            if (!string.IsNullOrEmpty(path))
            {
                var newSettings = ScriptableObject.CreateInstance<BNavGlobalSettings>();

                // Add some default groups
                newSettings.AddGroup("Default");
                newSettings.AddGroup("Game");
                newSettings.AddGroup("Menu");

                AssetDatabase.CreateAsset(newSettings, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Set as default
                SetDefaultGlobalSettings(newSettings);

                // Select the new asset
                Selection.activeObject = newSettings;
                EditorGUIUtility.PingObject(newSettings);
            }
        }

#endif
    }
}