using System.Collections.Generic;

namespace BTools.BNav
{
    /// <summary>
    /// Global manager for all BNavigation components
    /// </summary>
    public static class BNavManager
    {
        /// <summary>
        /// Global dictionary storing all BNavigation components by group
        /// </summary>
        private static readonly Dictionary<string, HashSet<BNavigation>> groupNavigationsMap = new Dictionary<string, HashSet<BNavigation>>();

        /// <summary>
        /// Map to track which group a BNavigation component belongs to
        /// </summary>
        private static readonly Dictionary<BNavigation, string> navigationGroupMap = new Dictionary<BNavigation, string>();

        private static BNavGlobalSettings GlobalSettings => BNavSettingsLoader.GlobalSettings;

        /// <summary>
        /// Add a BNavigation component to the global manager
        /// </summary>
        /// <param name="navigation">The BNavigation component to add</param>
        public static void AddNavigation(BNavigation navigation)
        {
            if (navigationGroupMap.TryGetValue(navigation, out var oldGroup) &&
                oldGroup != navigation.BelongGroup)
            {
                RemoveNavigation(navigation);
            }

            if (navigation == null || string.IsNullOrEmpty(navigation.BelongGroup))
            {
                return;
            }

            // Ensure the group exists in global settings
            var groupName = navigation.BelongGroup;
            if (!GlobalSettings.HasGroup(groupName))
            {
                return;
            }

            // Add to group dictionary
            if (!groupNavigationsMap.TryGetValue(groupName, out var navigations))
            {
                navigations = new HashSet<BNavigation>();
                groupNavigationsMap[groupName] = navigations;
            }

            if (!navigations.Contains(navigation))
            {
                navigations.Add(navigation);
            }

            // Update the navigation's group mapping
            navigationGroupMap[navigation] = groupName;
        }

        /// <summary>
        /// Remove a BNavigation component from the global manager
        /// </summary>
        /// <param name="navigation">The BNavigation component to remove</param>
        public static void RemoveNavigation(BNavigation navigation)
        {
            if (navigation == null || string.IsNullOrEmpty(navigation.BelongGroup))
            {
                return;
            }

            // Remove from group dictionary
            RemoveGroupNavigation(navigation.BelongGroup, navigation);

            // Remove from navigation group mapping
            if (navigationGroupMap.TryGetValue(navigation, out var cachedGroupName))
            {
                RemoveGroupNavigation(cachedGroupName, navigation);
                navigationGroupMap.Remove(navigation);
            }
        }

        /// <summary>
        /// Get navigations that can be reached from a specific group
        /// </summary>
        /// <param name="fromGroup">Source group name</param>
        /// <param name="globalSettings">Global settings for navigation rules</param>
        /// <returns>List of reachable BNavigation components</returns>
        /// <exception cref="System.ArgumentNullException">Null global settings Exception</exception>
        public static IEnumerable<BNavigation> EachReachableNavigation(string fromGroup, BNavGlobalSettings globalSettings)
        {
            if (globalSettings == null)
            {
                throw new System.ArgumentNullException(nameof(globalSettings), "Global settings cannot be null.");
            }

            foreach ((var targetGroup, var navigations) in groupNavigationsMap)
            {
                if (!globalSettings.CanNavigate(fromGroup, targetGroup))
                {
                    continue;
                }

                foreach (var navigation in navigations)
                {
                    yield return navigation;
                }
            }
        }

        /// <summary>
        /// Remove a BNavigation component from its group
        /// </summary>
        /// <param name="groupName">Name of the group to remove from</param>
        /// <param name="navigation">The BNavigation component to remove</param>
        private static void RemoveGroupNavigation(string groupName, BNavigation navigation)
        {
            if (string.IsNullOrEmpty(groupName) || navigation == null)
            {
                return;
            }

            // Remove from group dictionary
            if (!groupNavigationsMap.TryGetValue(groupName, out var navigations))
            {
                return;
            }

            navigations.Remove(navigation);

            // Clean up empty groups
            if (navigations.Count == 0)
            {
                groupNavigationsMap.Remove(groupName);
            }
        }
    }
}