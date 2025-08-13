using UnityEngine;

namespace BTools.BNav
{
    /// <summary>
    /// Runtime settings loader for BNav
    /// </summary>
    public static class BNavSettingsLoader
    {
        private static BNavGlobalSettings globalSettings;

        public static BNavGlobalSettings GlobalSettings
        {
            get
            {
                if (globalSettings == null)
                {
                    // Try to load from Resources folder
                    globalSettings = Resources.Load<BNavGlobalSettings>("BNavGlobalSettings");

                    // If not found, create a basic runtime instance
                    if (globalSettings == null)
                    {
                        globalSettings = CreateRuntimeDefaultSettings();
                    }
                }

                return globalSettings;
            }

            set
            {
                globalSettings = value;
            }
        }

        /// <summary>
        /// Create basic runtime default settings
        /// </summary>
        /// <returns>Basic global settings instance</returns>
        private static BNavGlobalSettings CreateRuntimeDefaultSettings()
        {
            var settings = ScriptableObject.CreateInstance<BNavGlobalSettings>();

            // Add basic default groups
            settings.AddGroup("Default");
            settings.AddGroup("Menu");
            settings.AddGroup("Game");

            return settings;
        }
    }
}