using UnityEditor;
using UnityEngine;

namespace BTools.BNav.Editor
{
    /// <summary>
    /// Initialize BNav settings when Unity starts
    /// </summary>
    [InitializeOnLoad]
    public static class BNavInitializer
    {
        static BNavInitializer()
        {
            // Initialize default settings on Unity startup
            EditorApplication.delayCall += InitializeSettings;
        }

        private static void InitializeSettings()
        {
            // Ensure default settings are loaded
            var defaultSettings = BNavProjectSettings.GetDefaultGlobalSettings();
            if (defaultSettings == null)
            {
                Debug.LogWarning("BNav: No default global settings found. Consider creating one in Project Settings > BNav Settings.");
            }
        }
    }
}