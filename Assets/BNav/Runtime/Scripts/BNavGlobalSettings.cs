using System;
using System.Collections.Generic;
using UnityEngine;

namespace BTools.BNav
{
    /// <summary>
    /// Settings for a navigation group, defining which other groups it can navigate to
    /// </summary>
    [Serializable]
    public class GroupSettings
    {
        [Tooltip("Name of the group (supports parent/child hierarchy with '/' separator)")]
        public string groupName = "";

        [Tooltip("List of groups that this group can navigate to")]
        public List<string> reachableGroups = new List<string>();

        /// <summary>
        /// Constructor for GroupSettings
        /// </summary>
        /// <param name="name">The name of the group</param>
        public GroupSettings(string name = "")
        {
            groupName = name;
            reachableGroups = new List<string>();
        }

        /// <summary>
        /// Check if this group can navigate to the specified target group
        /// </summary>
        /// <param name="targetGroup">The target group to check</param>
        /// <returns>True if navigation is allowed</returns>
        public bool CanNavigateTo(string targetGroup)
        {
            if (string.IsNullOrEmpty(targetGroup) || string.IsNullOrEmpty(groupName))
            {
                return false;
            }

            return reachableGroups.Contains(targetGroup);
        }
    }

    /// <summary>
    /// Global settings for BNav navigation system
    /// </summary>
    [CreateAssetMenu(fileName = "BNavGlobalSettings", menuName = "BTools/BNav/Global Settings")]
    public class BNavGlobalSettings : ScriptableObject
    {
        [Tooltip("List of all group settings")]
        [SerializeField]
        private List<GroupSettings> groupSettingsList = new List<GroupSettings>();

        private Dictionary<string, GroupSettings> groupNameSettingsMap = null;

        private Dictionary<string, GroupSettings> GroupNameSettingsMap
        {
            get
            {
                if (groupNameSettingsMap == null)
                {
                    groupNameSettingsMap = new Dictionary<string, GroupSettings>();
                    foreach (var groupSettings in groupSettingsList)
                    {
                        groupNameSettingsMap[groupSettings.groupName] = groupSettings;
                    }
                }

                return groupNameSettingsMap;
            }
        }

        /// <summary>
        /// Get settings for a specific group
        /// </summary>
        /// <param name="groupName">Name of the group</param>
        /// <returns>GroupSettings for the specified group, or null if not found</returns>
        public GroupSettings GetGroupSettings(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                return null;
            }

            if (GroupNameSettingsMap.TryGetValue(groupName, out var groupSettings))
            {
                return groupSettings;
            }

            return null;
        }

        /// <summary>
        /// Try to get settings for a specific group
        /// </summary>
        /// <param name="groupName">Name of the group</param>
        /// <param name="groupSettings">Output parameter for the found GroupSettings</param>
        /// <returns>True if the group was found, false otherwise</returns>
        public bool TryGetGroupSettings(string groupName, out GroupSettings groupSettings)
        {
            groupSettings = null;

            if (string.IsNullOrEmpty(groupName))
            {
                return false;
            }

            return GroupNameSettingsMap.TryGetValue(groupName, out groupSettings);
        }

        /// <summary>
        /// Check if a group with the specified name exists
        /// </summary>
        /// <param name="groupName">Name of the group to check</param>
        /// <returns>True if the group exists, false otherwise</returns>
        public bool HasGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                return false;
            }

            return GroupNameSettingsMap.ContainsKey(groupName);
        }

        /// <summary>
        /// Add a new group with default settings
        /// </summary>
        /// <param name="groupName">Name of the new group</param>
        /// <returns>The created GroupSettings</returns>
        public GroupSettings AddGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                return null;
            }

            // Check if group already exists
            var existingGroup = GetGroupSettings(groupName);
            if (existingGroup != null)
            {
                return existingGroup;
            }

            var newGroup = new GroupSettings(groupName);
            groupSettingsList.Add(newGroup);
            GroupNameSettingsMap[groupName] = newGroup;

            return newGroup;
        }

        /// <summary>
        /// Remove a group by name
        /// </summary>
        /// <param name="groupName">Name of the group to remove</param>
        /// <returns>True if the group was removed</returns>
        public bool RemoveGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                return false;
            }

            var groupToRemove = GetGroupSettings(groupName);
            if (groupToRemove != null)
            {
                groupSettingsList.Remove(groupToRemove);
                GroupNameSettingsMap.Remove(groupName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get all group names
        /// </summary>
        /// <param name="groupNames">List of all group names</param>
        public void GetAllGroupNames(ref List<string> groupNames)
        {
            if (groupNames == null)
            {
                groupNames = new List<string>();
            }
            else
            {
                groupNames.Clear();
            }

            foreach (var group in groupSettingsList)
            {
                if (!string.IsNullOrEmpty(group.groupName))
                {
                    groupNames.Add(group.groupName);
                }
            }
        }

        /// <summary>
        /// Check if navigation is allowed between two groups
        /// </summary>
        /// <param name="fromGroup">Source group</param>
        /// <param name="toGroup">Target group</param>
        /// <returns>True if navigation is allowed</returns>
        public bool CanNavigate(string fromGroup, string toGroup)
        {
            if (string.IsNullOrEmpty(fromGroup) || string.IsNullOrEmpty(toGroup))
            {
                return false;
            }

            // Same group is always navigable
            if (fromGroup == toGroup)
            {
                return true;
            }

            var sourceGroupSettings = GetGroupSettings(fromGroup);
            if (sourceGroupSettings == null)
            {
                return false; 
            }

            return sourceGroupSettings.CanNavigateTo(toGroup);
        }

        /// <summary>
        /// Clear all cached maps to force reloading
        /// </summary>
        public void ClearMapCaches()
        {
            groupNameSettingsMap = null;
        }

        /// <summary>
        /// Unity calls this method when the ScriptableObject is loaded or reset
        /// </summary>
        private void Reset()
        {
            ClearMapCaches();
        }
    }
}