using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BTools.BNav.Editor
{
    /// <summary>
    /// Custom editor for BNavGlobalSettings ScriptableObject
    /// </summary>
    [CustomEditor(typeof(BNavGlobalSettings))]
    public class BNavGlobalSettingsEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Property names for FindPropertyRelative
        /// </summary>
        private static class PropNames
        {
            public static readonly string GroupSettingsListName = "groupSettingsList";
            public static readonly string GroupName = "groupName";
            public static readonly string ReachableGroups = "reachableGroups";
        }

        private static readonly GUIContent ContentReachableGroups = new GUIContent("Reachable Groups", "List of groups that this group can navigate to");
        private static readonly GUIContent ContentAddReachableGroup = new GUIContent("Add Reachable", "Add a group that this group can navigate to");

        private SerializedProperty groupSettingsListProp;
        private BNavGlobalSettings targetSettings;

        private Vector2 scrollPosition;
        private string addGroupNewGroupName = "";
        private bool[] groupFoldouts;
        private bool addGroupFoldout;
        private bool updateGroupNameFoldout;
        private List<string> allGroupNames = new List<string>();
        private int updateGroupNameSelectedGroupIndex = 0;
        private string updateGroupNameNewName = "";
        private string[] updateGroupNameGroupOptions;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawGroupListSection();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            DrawAddGroupSection();
            DrawUpdateGroupNameSection();

            serializedObject.ApplyModifiedProperties();

            // Refresh group data if changes were made
            if (GUI.changed)
            {
                RefreshGroupData();
            }
        }

        /// <summary>
        /// Sorts a SerializedProperty array by a string property.
        /// </summary>
        /// <param name="arrayProp">The array property to sort</param>
        /// <param name="stringPropName">The string property name to sort by</param>
        /// <param name="descending">Sort descending if true, ascending if false (default)</param>
        private static void SortSerializedArrayByStringProperty(
            SerializedProperty arrayProp,
            string stringPropName,
            bool descending = false)
        {
            var count = arrayProp.arraySize;

            // Selection sort using MoveArrayElement
            for (var sortedIdx = 0; sortedIdx < count - 1; sortedIdx++)
            {
                // Find the element that should be at sortedIdx
                var bestIdx = sortedIdx;
                var bestValue = arrayProp.GetArrayElementAtIndex(sortedIdx).FindPropertyRelative(stringPropName).stringValue;

                for (var searchIdx = sortedIdx + 1; searchIdx < count; searchIdx++)
                {
                    var currentValue = arrayProp.GetArrayElementAtIndex(searchIdx).FindPropertyRelative(stringPropName).stringValue;
                    var shouldSwap = descending ?
                        string.Compare(currentValue, bestValue, System.StringComparison.Ordinal) > 0 :
                        string.Compare(currentValue, bestValue, System.StringComparison.Ordinal) < 0;

                    if (shouldSwap)
                    {
                        bestIdx = searchIdx;
                        bestValue = currentValue;
                    }
                }

                // Move the best element to the sorted position
                if (bestIdx != sortedIdx)
                {
                    arrayProp.MoveArrayElement(bestIdx, sortedIdx);
                }
            }
        }

        private void OnEnable()
        {
            targetSettings = (BNavGlobalSettings)target;
            groupSettingsListProp = serializedObject.FindProperty(PropNames.GroupSettingsListName);

            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            RefreshGroupData();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        #region Group List Section

        private void DrawGroupListSection()
        {
            EditorGUILayout.LabelField($"Groups ({groupSettingsListProp.arraySize})", EditorStyles.boldLabel);

            if (groupSettingsListProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No groups defined. Add a group above to get started.", MessageType.Info);
                return;
            }

            // Ensure foldouts array is correctly sized
            if (groupFoldouts == null || groupFoldouts.Length != groupSettingsListProp.arraySize)
            {
                groupFoldouts = new bool[groupSettingsListProp.arraySize];
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (var i = 0; i < groupSettingsListProp.arraySize; i++)
            {
                DrawGroupElement(i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroupElement(int index)
        {
            if (index >= groupSettingsListProp.arraySize)
            {
                return;
            }

            var groupProp = groupSettingsListProp.GetArrayElementAtIndex(index);
            var groupNameProp = groupProp.FindPropertyRelative(PropNames.GroupName);
            var reachableGroupsProp = groupProp.FindPropertyRelative(PropNames.ReachableGroups);

            var groupName = groupNameProp.stringValue;
            if (string.IsNullOrEmpty(groupName))
            {
                groupName = $"Group {index}";
            }

            using (var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Group header with foldout and delete button
                using (var horizontalScope = new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(15);

                    groupFoldouts[index] = EditorGUILayout.Foldout(groupFoldouts[index], groupName, true);

                    GUILayout.FlexibleSpace();

                    // Delete button
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("✖", GUILayout.Width(20)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Group",
                            $"Are you sure you want to delete group '{groupName}'?",
                            "Delete", "Cancel"))
                        {
                            RemoveGroup(index);
                            return;
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }

                if (groupFoldouts[index])
                {
                    EditorGUI.indentLevel++;

                    // Reachable groups section
                    EditorGUILayout.LabelField(ContentReachableGroups, EditorStyles.boldLabel);

                    if (reachableGroupsProp.arraySize == 0)
                    {
                        EditorGUILayout.HelpBox("This group cannot navigate to any other groups yet.", MessageType.Info);
                    }

                    // List current reachable groups
                    for (var j = reachableGroupsProp.arraySize - 1; j >= 0; j--)
                    {
                        var reachableGroupProp = reachableGroupsProp.GetArrayElementAtIndex(j);

                        using (var reachableHorizontalScope = new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("→", GUILayout.Width(30));
                            GUILayout.Space(-15);
                            EditorGUILayout.LabelField(reachableGroupProp.stringValue);

                            if (GUILayout.Button("Remove", GUILayout.Width(60)))
                            {
                                Undo.RecordObject(target, "Remove Reachable Group");
                                reachableGroupsProp.DeleteArrayElementAtIndex(j);
                            }
                        }
                    }

                    // Add reachable group dropdown
                    EditorGUILayout.Space();
                    using (var addReachableScope = new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(ContentAddReachableGroup, GUILayout.Width(140));

                        // Get available groups (excluding current group and already reachable groups)
                        var availableGroups = GetAvailableGroupsForReachable(groupNameProp.stringValue, reachableGroupsProp);

                        if (availableGroups.Count > 0)
                        {
                            var selectedIndex = EditorGUILayout.Popup(0, availableGroups.ToArray());
                            if (selectedIndex > 0)
                            {
                                Undo.RecordObject(target, "Add Reachable Group");

                                reachableGroupsProp.arraySize++;
                                var newReachableProp = reachableGroupsProp.GetArrayElementAtIndex(reachableGroupsProp.arraySize - 1);
                                newReachableProp.stringValue = availableGroups[selectedIndex];

                                SortReachableGroups(reachableGroupsProp);
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField("No available groups", EditorStyles.helpBox);
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }

        #endregion Group List Section

        #region Add Group Section

        private void DrawAddGroupSection()
        {
            using (var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (var horizontalScope = new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(15);
                    addGroupFoldout = EditorGUILayout.Foldout(addGroupFoldout, "Add New Group", toggleOnLabelClick: true);
                }

                if (!addGroupFoldout)
                {
                    return;
                }

                EditorGUI.indentLevel++;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Group Name", GUILayout.Width(100));
                    addGroupNewGroupName = EditorGUILayout.TextField(addGroupNewGroupName);

                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(addGroupNewGroupName)))
                    {
                        if (GUILayout.Button("Add", GUILayout.Width(60)))
                        {
                            AddNewGroup(addGroupNewGroupName.Trim());
                            addGroupNewGroupName = "";
                            GUI.FocusControl(null);
                        }
                    }
                }

                EditorGUILayout.HelpBox(
                    "Group names can use '/' for hierarchical organization (e.g., 'Menu/Settings').\n" +
                    "Navigation rules are defined by the reachable groups list for each group.",
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }
        }

        private void AddNewGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return;
            }

            // Check if group already exists
            if (targetSettings.HasGroup(groupName))
            {
                EditorUtility.DisplayDialog("Group Exists", $"A group named '{groupName}' already exists.", "OK");
                return;
            }

            Undo.RecordObject(target, "Add New Group");

            targetSettings.AddGroup(groupName);

            serializedObject.Update();
            groupSettingsListProp = serializedObject.FindProperty(PropNames.GroupSettingsListName);

            SortSerializedArrayByStringProperty(groupSettingsListProp, PropNames.GroupName);
            RefreshGroupData();
        }

        #endregion Add Group Section

        #region Update Group Name Section

        private void DrawUpdateGroupNameSection()
        {
            using (var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (var horizontalScope = new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(15);
                    updateGroupNameFoldout = EditorGUILayout.Foldout(updateGroupNameFoldout, "Update Group Name", toggleOnLabelClick: true);
                }
                if (!updateGroupNameFoldout)
                {
                    return;
                }

                EditorGUI.indentLevel++;

                var isPlayMode = EditorApplication.isPlaying;

                using (new EditorGUI.DisabledScope(isPlayMode))
                {
                    if (groupSettingsListProp.arraySize == 0)
                    {
                        EditorGUILayout.HelpBox("No groups available to update.", MessageType.Info);
                        EditorGUI.indentLevel--;

                        return;
                    }

                    // Current group dropdown
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Current Name", GUILayout.Width(100));

                        // Ensure selectedGroupIndex is valid
                        if (updateGroupNameSelectedGroupIndex >= updateGroupNameGroupOptions.Length)
                        {
                            updateGroupNameSelectedGroupIndex = 0;
                        }

                        EditorGUI.BeginChangeCheck();
                        updateGroupNameSelectedGroupIndex = EditorGUILayout.Popup(updateGroupNameSelectedGroupIndex, updateGroupNameGroupOptions);
                        if (EditorGUI.EndChangeCheck())
                        {
                            // Clear the new name when selection changes
                            updateGroupNameNewName = "";
                            GUI.FocusControl(null);
                        }

                        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                        {
                            RefreshGroupOptions();
                        }
                    }

                    // New group name field
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("New Name", GUILayout.Width(100));
                        updateGroupNameNewName = EditorGUILayout.TextField(updateGroupNameNewName);

                        var canUpdate =
                            updateGroupNameSelectedGroupIndex > 0 &&
                            updateGroupNameSelectedGroupIndex < updateGroupNameGroupOptions.Length &&
                            !string.IsNullOrWhiteSpace(updateGroupNameNewName);

                        // Update button
                        using (new EditorGUI.DisabledScope(!canUpdate))
                        {
                            if (GUILayout.Button("Update", GUILayout.Width(60)))
                            {
                                var oldGroupName = updateGroupNameGroupOptions[updateGroupNameSelectedGroupIndex];
                                var newGroupName = updateGroupNameNewName.Trim();

                                if (ShowUpdateGroupNameWarning(oldGroupName, newGroupName))
                                {
                                    UpdateGroupName(oldGroupName, newGroupName);
                                }
                            }
                        }
                    }

                    if (!isPlayMode)
                    {
                        EditorGUILayout.HelpBox(
                            "Change group name operation will update all references to the group name in scenes and prefabs. " +
                            "Make sure to backup your project before proceeding.",
                            MessageType.Info);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private bool ShowUpdateGroupNameWarning(string oldGroupName, string newGroupName)
        {
            return EditorUtility.DisplayDialog(
                "Update Group Name",
                $"This will update group name from '{oldGroupName}' to '{newGroupName}'.\n\n" +
                "The following operations will be performed:\n" +
                "• Update group settings in this asset\n" +
                "• Update all reachable group references\n" +
                "• Search and update all BNavigation components in scenes\n" +
                "• Search and update all BNavigation components in prefabs\n" +
                "• Force script recompilation\n\n" +
                "This operation cannot be undone. \nContinue?",
                "Continue",
                "Cancel");
        }

        private void UpdateGroupName(string oldGroupName, string newGroupName)
        {
            // Validate inputs
            if (!ValidateUpdateGroupName(oldGroupName, newGroupName))
            {
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Updating Group Name", "Preparing...", 0f);

                // Clear map caches to ensure no stale data
                targetSettings.ClearMapCaches();

                // Update the group settings
                UpdateGroupSettingsName(oldGroupName, newGroupName);
                EditorUtility.DisplayProgressBar("Updating Group Name", "Updated group settings", 0.2f);

                // Update reachable groups references
                UpdateReachableGroupsReferences(oldGroupName, newGroupName);
                EditorUtility.DisplayProgressBar("Updating Group Name", "Updated reachable groups", 0.4f);

                // Update prefabs
                UpdatePrefabsGroupReferences(oldGroupName, newGroupName);
                EditorUtility.DisplayProgressBar("Updating Group Name", "Updated prefabs", 0.7f);

                // Update scenes
                UpdateScenesGroupReferences(oldGroupName, newGroupName);
                EditorUtility.DisplayProgressBar("Updating Group Name", "Updated scenes", 0.9f);

                // Force recompilation and refresh
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayProgressBar("Updating Group Name", "Forcing recompilation...", 1f);
                EditorUtility.RequestScriptReload();

                // Clear update fields
                updateGroupNameSelectedGroupIndex = 0;
                updateGroupNameNewName = "";

                // Refresh data
                serializedObject.Update();
                groupSettingsListProp = serializedObject.FindProperty(PropNames.GroupSettingsListName);
                RefreshGroupData();

                Debug.Log($"Successfully updated group name from '{oldGroupName}' to '{newGroupName}'");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update group name: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to update group name: {e.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private bool ValidateUpdateGroupName(string oldGroupName, string newGroupName)
        {
            // Check if old group exists
            if (!targetSettings.HasGroup(oldGroupName))
            {
                EditorUtility.DisplayDialog("Error", $"Group '{oldGroupName}' does not exist.", "OK");
                return false;
            }

            // Check if new group name already exists
            if (targetSettings.HasGroup(newGroupName))
            {
                EditorUtility.DisplayDialog("Error", $"Group '{newGroupName}' already exists.", "OK");
                return false;
            }

            return true;
        }

        private void UpdateGroupSettingsName(string oldGroupName, string newGroupName)
        {
            // Find and update the group settings
            for (var i = 0; i < groupSettingsListProp.arraySize; i++)
            {
                var groupProp = groupSettingsListProp.GetArrayElementAtIndex(i);
                var groupNameProp = groupProp.FindPropertyRelative(PropNames.GroupName);

                if (groupNameProp.stringValue == oldGroupName)
                {
                    groupNameProp.stringValue = newGroupName;
                    break;
                }
            }

            // Sort the list after updating
            SortSerializedArrayByStringProperty(groupSettingsListProp, PropNames.GroupName);
            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateReachableGroupsReferences(string oldGroupName, string newGroupName)
        {
            // Update all reachable groups references
            for (var i = 0; i < groupSettingsListProp.arraySize; i++)
            {
                var groupProp = groupSettingsListProp.GetArrayElementAtIndex(i);
                var reachableGroupsProp = groupProp.FindPropertyRelative(PropNames.ReachableGroups);
                var isChanged = false;

                for (var j = 0; j < reachableGroupsProp.arraySize; j++)
                {
                    var reachableGroupProp = reachableGroupsProp.GetArrayElementAtIndex(j);
                    if (reachableGroupProp.stringValue == oldGroupName)
                    {
                        reachableGroupProp.stringValue = newGroupName;
                        isChanged = true;
                    }
                }

                // Sort reachable groups for this group
                if (isChanged)
                {
                    SortReachableGroups(reachableGroupsProp);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdatePrefabsGroupReferences(string oldGroupName, string newGroupName)
        {
            // Find all prefab assets
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            var prefabPaths = new List<string>();

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                prefabPaths.Add(path);
            }

            // Update each prefab
            for (var i = 0; i < prefabPaths.Count; i++)
            {
                var prefabPath = prefabPaths[i];
                var progress = 0.9f + (0.1f * i / prefabPaths.Count);
                EditorUtility.DisplayProgressBar("Updating Group Name", $"Updating prefab: {Path.GetFileName(prefabPath)}", progress);

                UpdatePrefabGroupReferences(prefabPath, oldGroupName, newGroupName);
            }
        }

        private void UpdatePrefabGroupReferences(string prefabPath, string oldGroupName, string newGroupName)
        {
            // Load the prefab
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                return;
            }

            // Check if prefab has BNavigation components
            var bNavs = prefabAsset.GetComponentsInChildren<BNavigation>(true);
            var prefabModified = false;
            var modifiedCount = 0;

            foreach (var bNav in bNavs)
            {
                if (bNav.BelongGroup == oldGroupName)
                {
                    // Use PrefabUtility to edit the prefab
                    var prefabAssetPath = AssetDatabase.GetAssetPath(prefabAsset);

                    // Load prefab contents for editing
                    var prefabContents = PrefabUtility.LoadPrefabContents(prefabAssetPath);

                    try
                    {
                        // Find and update BNavigation components in prefab contents
                        var prefabBNavs = prefabContents.GetComponentsInChildren<BNavigation>(true);
                        foreach (var prefabBNav in prefabBNavs)
                        {
                            if (prefabBNav.BelongGroup == oldGroupName)
                            {
                                prefabBNav.BelongGroup = newGroupName;
                                prefabModified = true;
                                modifiedCount++;
                            }
                        }

                        // Save the prefab if modified
                        if (prefabModified)
                        {
                            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabAssetPath);
                            Debug.Log($"Updated {modifiedCount} BNavigation components in prefab '{prefabAsset.name}' from '{oldGroupName}' to '{newGroupName}'");
                        }
                    }
                    finally
                    {
                        // Always unload prefab contents
                        PrefabUtility.UnloadPrefabContents(prefabContents);
                    }

                    break; // Only need to update once per prefab
                }
            }
        }

        private void UpdateScenesGroupReferences(string oldGroupName, string newGroupName)
        {
            // Get all scene paths
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            var scenePaths = new List<string>();

            foreach (var guid in sceneGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                scenePaths.Add(path);
            }

            // Update each scene
            for (var i = 0; i < scenePaths.Count; i++)
            {
                var scenePath = scenePaths[i];
                var progress = 0.7f + (0.2f * i / scenePaths.Count);
                EditorUtility.DisplayProgressBar("Updating Group Name", $"Updating scene: {Path.GetFileName(scenePath)}", progress);

                UpdateSceneGroupReferences(scenePath, oldGroupName, newGroupName);
            }
        }

        private void UpdateSceneGroupReferences(string scenePath, string oldGroupName, string newGroupName)
        {
            // Load the scene additively to preserve current scene
            var currentScene = SceneManager.GetActiveScene();
            var targetScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                // Find all BNavigation components in the scene (including inactive objects)
                var allBNavs = Resources.FindObjectsOfTypeAll<BNavigation>()
                    .Where(nav => nav.gameObject.scene == targetScene)
                    .ToArray();

                var sceneModified = false;
                var modifiedCount = 0;

                foreach (var bNav in allBNavs)
                {
                    if (bNav.BelongGroup == oldGroupName)
                    {
                        Undo.RecordObject(bNav, "Update Group Name");
                        bNav.BelongGroup = newGroupName;
                        EditorUtility.SetDirty(bNav);
                        sceneModified = true;
                        modifiedCount++;
                    }
                }

                // Save the scene if modified
                if (sceneModified)
                {
                    EditorSceneManager.SaveScene(targetScene);
                    Debug.Log($"Updated {modifiedCount} BNavigation components in scene '{targetScene.name}' from '{oldGroupName}' to '{newGroupName}'");
                }
            }
            finally
            {
                // Close the scene if it's not the current scene
                if (targetScene != currentScene)
                {
                    EditorSceneManager.CloseScene(targetScene, removeScene: true);
                }
            }
        }

        #endregion Update Group Name Section

        private void OnUndoRedoPerformed()
        {
            serializedObject.Update();
            groupSettingsListProp = serializedObject.FindProperty(PropNames.GroupSettingsListName);

            targetSettings.ClearMapCaches();

            RefreshGroupData();
        }

        private void SortReachableGroups(SerializedProperty reachableGroupsProp)
        {
            // Sort reachableGroupsProp by stringValue in descending order

            var reachableNames = new List<string>();
            for (var k = 0; k < reachableGroupsProp.arraySize; k++)
            {
                reachableNames.Add(reachableGroupsProp.GetArrayElementAtIndex(k).stringValue);
            }

            reachableNames.Sort((a, b) => string.Compare(b, a)); // descending

            for (var k = 0; k < reachableNames.Count; k++)
            {
                reachableGroupsProp.GetArrayElementAtIndex(k).stringValue = reachableNames[k];
            }
        }

        private void RemoveGroup(int index)
        {
            if (index < 0 || index >= groupSettingsListProp.arraySize)
            {
                return;
            }

            Undo.RecordObject(target, "Remove Group");

            var groupProp = groupSettingsListProp.GetArrayElementAtIndex(index);
            var groupNameProp = groupProp.FindPropertyRelative(PropNames.GroupName);
            var removedGroupName = groupNameProp.stringValue;

            targetSettings.RemoveGroup(removedGroupName);

            serializedObject.Update();
            groupSettingsListProp = serializedObject.FindProperty(PropNames.GroupSettingsListName);

            // Remove references to this group from other groups' reachable lists
            for (var i = 0; i < groupSettingsListProp.arraySize; i++)
            {
                var otherGroupProp = groupSettingsListProp.GetArrayElementAtIndex(i);
                var otherReachableGroupsProp = otherGroupProp.FindPropertyRelative(PropNames.ReachableGroups);

                for (var j = otherReachableGroupsProp.arraySize - 1; j >= 0; j--)
                {
                    var reachableGroupProp = otherReachableGroupsProp.GetArrayElementAtIndex(j);
                    if (reachableGroupProp.stringValue == removedGroupName)
                    {
                        otherReachableGroupsProp.DeleteArrayElementAtIndex(j);
                    }
                }
            }

            RefreshGroupData();

            // Ask user if they want to list all BNavigation components using this group
            if (EditorUtility.DisplayDialog("List BNavigation References",
                $"Do you want to list all BNavigation components that were using the deleted group '{removedGroupName}' to the console?\n\n" +
                "This will search through all scenes and prefabs in the project.",
                "Yes", "No"))
            {
                ListBNavigationReferences(removedGroupName);
            }
        }

        private List<string> GetAvailableGroupsForReachable(string currentGroupName, SerializedProperty reachableGroupsProp)
        {
            var available = new List<string> { "Select Group..." };

            // Get all current reachable groups
            var currentReachable = new HashSet<string>();
            for (int i = 0; i < reachableGroupsProp.arraySize; i++)
            {
                currentReachable.Add(reachableGroupsProp.GetArrayElementAtIndex(i).stringValue);
            }

            // Add groups that are not current group and not already reachable
            foreach (var groupName in allGroupNames)
            {
                if (groupName != currentGroupName && !currentReachable.Contains(groupName))
                {
                    available.Add(groupName);
                }
            }

            return available;
        }

        private void RefreshGroupData()
        {
            allGroupNames.Clear();
            targetSettings.GetAllGroupNames(ref allGroupNames);

            // Ensure foldouts array is correctly sized
            if (groupFoldouts == null || groupFoldouts.Length != groupSettingsListProp.arraySize)
            {
                var newFoldouts = new bool[groupSettingsListProp.arraySize];
                if (groupFoldouts != null)
                {
                    // Copy existing foldout states
                    for (var i = 0; i < Mathf.Min(groupFoldouts.Length, newFoldouts.Length); i++)
                    {
                        newFoldouts[i] = groupFoldouts[i];
                    }
                }

                groupFoldouts = newFoldouts;
            }

            RefreshGroupOptions();
        }

        private void RefreshGroupOptions()
        {
            var options = new List<string> { "Select Group..." };
            options.AddRange(allGroupNames);
            updateGroupNameGroupOptions = options.ToArray();

            // Ensure selectedGroupIndex is valid
            if (updateGroupNameSelectedGroupIndex >= updateGroupNameGroupOptions.Length)
            {
                updateGroupNameSelectedGroupIndex = 0;
            }
        }

        private void ListBNavigationReferences(string groupName)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Searching BNavigation References", "Preparing...", 0f);

                var stringBuilder = new System.Text.StringBuilder();

                // Search in prefabs
                var prefabCount = SearchBNavigationInPrefabs(groupName, stringBuilder);
                stringBuilder.AppendLine();

                EditorUtility.DisplayProgressBar("Searching BNavigation References", "Generating report...", 0.5f);

                // Search in scenes
                var sceneCount = SearchBNavigationInScenes(groupName, stringBuilder);
                EditorUtility.DisplayProgressBar("Searching BNavigation References", "Searching in prefabs...", 0.9f);

                // Generate final report
                var totalCount = prefabCount + sceneCount;
                var reportBuilder = new System.Text.StringBuilder();

                reportBuilder.AppendLine($"<b>BNavigation References Report for Group: <color=yellow>'{groupName}'</color></b> (See below for details)");
                reportBuilder.AppendLine($"<b>Total Found: {totalCount}</b> (Prefabs: {prefabCount}, Scenes: {sceneCount})");
                reportBuilder.AppendLine();

                if (totalCount > 0)
                {
                    reportBuilder.Append(stringBuilder.ToString());
                }
                else
                {
                    reportBuilder.AppendLine("<color=green>No BNavigation components found using this group name.</color>");
                }

                Debug.Log(reportBuilder.ToString());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to search BNavigation references: {e.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private int SearchBNavigationInPrefabs(string groupName, System.Text.StringBuilder stringBuilder)
        {
            var prefabCount = 0;
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            var prefabPaths = new List<string>();

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                prefabPaths.Add(path);
            }

            if (prefabPaths.Count > 0)
            {
                stringBuilder.AppendLine("<b>Prefabs:</b>");
            }

            for (var i = 0; i < prefabPaths.Count; i++)
            {
                var prefabPath = prefabPaths[i];
                var progress = 0.5f + (0.4f * i / prefabPaths.Count);

                EditorUtility.DisplayProgressBar("Searching BNavigation References",
                    $"Searching in prefab: {Path.GetFileName(prefabPath)}", progress);

                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabAsset == null)
                {
                    continue;
                }

                var bNavs = prefabAsset.GetComponentsInChildren<BNavigation>(true);
                foreach (var bNav in bNavs)
                {
                    if (bNav.BelongGroup != groupName)
                    {
                        continue;
                    }

                    prefabCount++;

                    var fileName = Path.GetFileName(prefabPath);
                    var objectPath = GetGameObjectPath(bNav.gameObject);

                    // Highlight the last part of the path
                    var objectPathSplit = objectPath.Split('/');
                    if (objectPathSplit.Length > 1)
                    {
                        var lastPath = objectPathSplit[objectPathSplit.Length - 1];

                        objectPathSplit[objectPathSplit.Length - 1] = $"<color=yellow>{lastPath}</color>";
                        objectPath = string.Join(" → ", objectPathSplit);
                    }
                    else
                    {
                        objectPath = $"<color=yellow>{objectPath}</color>";
                    }

                    stringBuilder.AppendLine($"{prefabCount}. <color=cyan>{fileName}</color>: {objectPath}");
                }
            }

            return prefabCount;
        }

        private int SearchBNavigationInScenes(string groupName, System.Text.StringBuilder stringBuilder)
        {
            var sceneCount = 0;
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            var scenePaths = new List<string>();

            foreach (var guid in sceneGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                scenePaths.Add(path);
            }

            if (scenePaths.Count > 0)
            {
                stringBuilder.AppendLine("<b>Scenes:</b>");
            }

            var currentScene = SceneManager.GetActiveScene();

            for (var i = 0; i < scenePaths.Count; i++)
            {
                var scenePath = scenePaths[i];
                var progress = 0.1f + (0.4f * i / scenePaths.Count);

                EditorUtility.DisplayProgressBar("Searching BNavigation References",
                    $"Searching in scene: {Path.GetFileName(scenePath)}", progress);

                var targetScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                try
                {
                    var allBNavs = Resources.FindObjectsOfTypeAll<BNavigation>()
                        .Where(nav => nav.gameObject.scene == targetScene && nav.BelongGroup == groupName)
                        .ToArray();

                    foreach (var bNav in allBNavs)
                    {
                        sceneCount++;

                        var fileName = Path.GetFileName(scenePath);
                        var objectPath = GetGameObjectPath(bNav.gameObject);

                        // Highlight the last part of the path
                        var objectPathSplit = objectPath.Split('/');
                        if (objectPathSplit.Length > 1)
                        {
                            var lastPath = objectPathSplit[objectPathSplit.Length - 1];

                            objectPathSplit[objectPathSplit.Length - 1] = $"<color=yellow>{lastPath}</color>";
                            objectPath = string.Join(" → ", objectPathSplit);
                        }
                        else
                        {
                            objectPath = $"<color=yellow>{objectPath}</color>";
                        }

                        stringBuilder.AppendLine($"{sceneCount}. <color=cyan>{fileName}</color>: {objectPath}");
                    }
                }
                finally
                {
                    if (targetScene != currentScene)
                    {
                        EditorSceneManager.CloseScene(targetScene, removeScene: true);
                    }
                }
            }

            return sceneCount;
        }

        private string GetGameObjectPath(GameObject gameObject)
        {
            var path = gameObject.name;
            var parent = gameObject.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}